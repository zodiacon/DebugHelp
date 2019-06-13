using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Zodiacon.DebugHelp {
	public sealed class SymbolHandler : IDisposable {
		static int _instances;
		IntPtr _hProcess;
		bool _ownHandle;

		private SymbolHandler(IntPtr hProcess, bool ownHandle) {
			_hProcess = hProcess;
			_ownHandle = ownHandle;
		}

		public static SymbolHandler CreateFromProcess(int pid, SymbolOptions options = SymbolOptions.None, string searchPath = null) {
			if (searchPath == null)
				searchPath = GetDefaultSearchPath();
			var handle = new IntPtr(pid);
			if(Win32.SymInitialize(handle, searchPath, true)) {
				return new SymbolHandler(handle, false);
			}
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public static SymbolHandler TryCreateFromProcess(int pid, SymbolOptions options = SymbolOptions.None, string searchPath = null) {
			if (searchPath == null)
				searchPath = GetDefaultSearchPath();
			var handle = new IntPtr(pid);
			if(Win32.SymInitialize(handle, searchPath, true)) {
				return new SymbolHandler(handle, false);
			}
			return null;
		}

		public static SymbolHandler CreateFromHandle(IntPtr handle, SymbolOptions options = SymbolOptions.None, string searchPath = null) {
			if (searchPath == null)
				searchPath = GetDefaultSearchPath();
			Win32.SymSetOptions(options);
			Win32.SymInitialize(handle, searchPath, true).ThrowIfWin32Failed();
			return new SymbolHandler(handle, false);
		}

		private static string GetDefaultSearchPath() {
			var path = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
			if (path != null) {
				int index = path.IndexOf('*');
				if (index < 0)
					return path;
				path = path.Substring(index + 1, path.IndexOf('*', index + 1) - index - 1);
				if (string.IsNullOrWhiteSpace(path))
					path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			}

			return path;
		}

		public static SymbolHandler Create(SymbolOptions options = SymbolOptions.CaseInsensitive | SymbolOptions.UndecorateNames, string searchPath = null) {
			if (Debugger.IsAttached)
				options |= SymbolOptions.Debug;

			if (searchPath == null)
				searchPath = GetDefaultSearchPath();
			Win32.SymSetOptions(options);
			var handle = new IntPtr(++_instances);
			Win32.SymInitialize(handle, searchPath, false).ThrowIfWin32Failed();
			return new SymbolHandler(handle, false);
		}

		public void Dispose() {
			Win32.SymCleanup(_hProcess);
			if(_ownHandle)
				Win32.CloseHandle(_hProcess);
		}

		public ulong LoadSymbolsForModule(string imageName, ulong dllBase = 0, string moduleName = null, IntPtr? hFile = null) {
			var address = Win32.SymLoadModuleEx(_hProcess, hFile ?? IntPtr.Zero, imageName, moduleName, dllBase, 0, IntPtr.Zero, 0);
			var error = Marshal.GetLastWin32Error();
			if(address == 0 && error != 0)
				throw new Win32Exception(error);
			return address;
		}

		public ulong TryLoadSymbolsForModule(string imageName, string moduleName = null, IntPtr? hFile = null) {
			var address = Win32.SymLoadModuleEx(_hProcess, hFile ?? IntPtr.Zero, imageName, moduleName, 0, 0, IntPtr.Zero, 0);
			return address;
		}

#pragma warning disable CSE0003 // Use expression-bodied members
		public Task<ulong> TryLoadSymbolsForModuleAsync(string imageName, ulong dllBase = 0, string moduleName = null, IntPtr? hFile = null) {
			return Task.Run(() => Win32.SymLoadModuleEx(_hProcess, hFile ?? IntPtr.Zero, imageName, moduleName, dllBase, 0, IntPtr.Zero, 0));
		}
#pragma warning restore CSE0003 // Use expression-bodied members

		public SymbolInfo GetSymbolFromAddress(ulong address, out ulong displacement) {
			var info = new SymbolInfo();
			info.Init();
			Win32.SymFromAddr(_hProcess, address, out displacement, ref info).ThrowIfWin32Failed();
			return info;
		}

		public bool TryGetSymbolFromAddress(ulong address, ref SymbolInfo symbol, out ulong displacement) {
			symbol.Init();
			return Win32.SymFromAddr(_hProcess, address, out displacement, ref symbol);
		}

		public IReadOnlyList<ModuleInfo> EnumModules() {
			List<ModuleInfo> modules = new List<ModuleInfo>(8);
			Win32.SymEnumerateModules64(_hProcess, (name, dllBase, context) => {
				modules.Add(new ModuleInfo { Name = name, Base = dllBase });
				return true;
			}, IntPtr.Zero).ThrowIfWin32Failed();
			return modules;
		}

		public bool GetSymbolFromName(string name, ref SymbolInfo symbol) {
			return Win32.SymFromName(_hProcess, name, ref symbol);
		}

		public bool GetSymbolFromIndex(ulong dllBase, int index, ref SymbolInfo symbol) {
			return Win32.SymFromIndex(_hProcess, dllBase, index, ref symbol);
		}

		public ICollection<SymbolInfo> EnumSymbols(ulong baseAddress, string mask = "*!*") {
			var symbols = new List<SymbolInfo>(16);

			Win32.SymEnumSymbols(_hProcess, baseAddress, mask, (ref SymbolInfo symbol, uint size, IntPtr context) => {
				symbols.Add(symbol);
				return true;
			}, IntPtr.Zero);
			return symbols;
		}

		public IList<SymbolInfo> EnumTypes(ulong baseAddress, string mask = "*") {
			var symbols = new List<SymbolInfo>(16);

			Win32.SymEnumTypesByName(_hProcess, baseAddress, mask, (ref SymbolInfo symbol, uint size, IntPtr context) => {
				symbols.Add(symbol);
				return true;
			}, IntPtr.Zero);
			return symbols;
		}

        public unsafe IList<SourceFile> EnumSourceFiles(ulong baseAddress, string mask = "*") {
            var files = new List<SourceFile>(4);
            Win32.SymEnumSourceFiles(_hProcess, baseAddress, mask, (source, context) => {
                files.Add(new SourceFile {
                    BaseAddress = source.ModuleBase,
                    FileName = new string(source.FileName)
                });
                return true;
            }, IntPtr.Zero);

            return files;
        }

		public int GetTypeIndexFromName(ulong baseAddress, string name) {
			var symbol = SymbolInfo.Create();
			return Win32.SymGetTypeFromName(_hProcess, baseAddress, name, ref symbol) ? symbol.TypeIndex : 0;
		}

		public bool GetTypeFromName(ulong baseAddress, string name, ref SymbolInfo type) 
			=> Win32.SymGetTypeFromName(_hProcess, baseAddress, name, ref type);

		public bool Refresh() => Win32.SymRefreshModuleList(_hProcess);

		public unsafe string GetTypeInfoName(ulong dllBase, int typeIndex) {
			if (Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Name, out char* name)) {
				var strName = new string(name);
				Marshal.FreeCoTaskMem(new IntPtr(name));
				return strName;
			}
			return null;
		}

		public unsafe SymbolTag GetSymbolTag(ulong dllBase, int typeIndex) {
			var tag = SymbolTag.Null;
			Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Tag, out tag);
			return tag;
		}

        public unsafe Variant GetSymbolValue(ulong dllBase, int typeIndex) {
            var value = new Variant();
            Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Value, out value);
            return value;
        }

        public ulong GetSymbolLength(ulong dllBase, int typeIndex) {
            Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Length, out ulong value);
            return value;
        }

        public int GetSymbolDataKind(ulong dllBase, int typeIndex) {
            Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.DataKind, out int value);
            return value;
        }

        public UdtKind GetSymbolUdtKind(ulong dllBase, int typeIndex) {
            if (Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.UdtKind, out int value)) {
                return (UdtKind)value;
            }
            return UdtKind.Unknown;
        }

        public int GetSymbolType(ulong dllBase, int typeIndex) {
            Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Type, out int value);
            return value;
        }

        public int GetSymbolBitPosition(ulong dllBase, int typeIndex) {
            if (Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.BitPosition, out int value))
                return value;
            return -1;
        }

        public int GetSymbolCount(ulong dllBase, int typeIndex) {
            int value = -1;
            Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Count, out value);
            return value;
        }

        public int GetSymbolAddressOffset(ulong dllBase, int typeIndex) {
            bool success = Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.AddressOffset, out int value);
            return value;
        }

        public BasicType GetSymbolBaseType(ulong dllBase, int typeIndex) {
            bool success = Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.BaseType, out int value);
            return (BasicType)value;
        }

        public int GetSymbolOffset(ulong dllBase, int typeIndex) {
            bool success = Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Offset, out int value);
            return value;
        }

        public int GetSymbolChildrenCount(ulong dllBase, int typeIndex) {
            bool success = Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.ChildrenCount, out int value);
            return value;
        }

        public unsafe StructDescriptor BuildStructDescriptor(ulong dllBase, int typeIndex) {
			if (Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.ChildrenCount, out int childrenCount)) {
				var structDesc = new StructDescriptor(childrenCount);
				if (Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.Length, out ulong size)) {
					structDesc.Length = (int)size;
				}
				var childrenParams = new FindChildrenParams { Count = childrenCount };
				structDesc.Length = (int)size;
				if (Win32.SymGetTypeInfo(_hProcess, dllBase, typeIndex, SymbolTypeInfo.FindChildren, ref childrenParams)) {
					for (var i = 0; i < childrenParams.Count; i++) {
						var sym = SymbolInfo.Create();
                        var child = childrenParams.Child[i];
                        if (GetSymbolFromIndex(dllBase, child, ref sym)) {
                            if (Win32.SymGetTypeInfo(_hProcess, dllBase, child, SymbolTypeInfo.Offset, out int offset) &&
                                Win32.SymGetTypeInfo(_hProcess, dllBase, child, SymbolTypeInfo.Tag, out SymbolTag tag)) {
                                sym.Tag = tag;
                                sym.TypeIndex = child;
                                var member = new StructMember(sym, offset);
                                structDesc.AddMember(member);
                            }
                            else if (Win32.SymGetTypeInfo(_hProcess, dllBase, child, SymbolTypeInfo.Value, out Variant value)) {
                                sym.Tag = SymbolTag.Enum;
                                sym.Value = value.lValue;
                                sym.TypeIndex = child;
                                var member = new StructMember(sym, 0);
                                switch (sym.Size) {
                                    case 8:
                                        member.Value = value.lValue;
                                        break;

                                    case 2:
                                        member.Value = value.sValue;
                                        break;

                                    case 1:
                                        member.Value = value.bValue;
                                        break;

                                    default:
                                        member.Value = value.iValue;
                                        break;
                                }
                                structDesc.AddMember(member);
                            }
                        }
					}
				}
				return structDesc;
			}
			return null;
		}
	}
}
