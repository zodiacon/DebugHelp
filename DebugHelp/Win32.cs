using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Zodiacon.DebugHelp {
	enum ProcessAccessMask : uint {
		Query = 0x400
	}

	delegate bool SymEnumerateModuleProc(string module, ulong dllBase, IntPtr context);
    delegate bool EnumSourceFilesCallback(SOURCEFILE sourceFile, IntPtr context);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    unsafe public struct SOURCEFILE {
        public ulong ModuleBase;
        public char* FileName;
    }

    public enum BasicType {
		NoType = 0,
		Void = 1,
		Char = 2,
		WChar = 3,
		Int = 6,
		UInt = 7,
		Float = 8,
		BCD = 9,
		Bool = 10,
		Long = 13,
		ULong = 14,
		Currency = 25,
		Date = 26,
		Variant = 27,
		Complex = 28,
		Bit = 29,
		BSTR = 30,
		Hresult = 31
	}

    public enum UdtKind {
        Struct,
        Class,
        Union,
        Interface,
        Unknown = 99
    }

    public enum SymbolTag {
		Null,
		Exe,
		Compiland,
		CompilandDetails,
		CompilandEnv,
		Function,
		Block,
		Data,
		Annotation,
		Label,
		PublicSymbol,
		UDT,
		Enum,
		FunctionType,
		PointerType,
		ArrayType,
		BaseType,
		Typedef,
		BaseClass,
		Friend,
		FunctionArgType,
		FuncDebugStart,
		FuncDebugEnd,
		UsingNamespace,
		VTableShape,
		VTable,
		Custom,
		Thunk,
		CustomType,
		ManagedType,
		Dimension,
		CallSite,
		InlineSite,
		BaseInterface,
		VectorType,
		MatrixType,
		HLSLType,
		Caller,
		Callee,
		Export,
		HeapAllocationSite,
		CoffGroup,
		Max
	}

	public enum SymbolTypeInfo {
		Tag,
		Name,
		Length,
		Type,
		TypeId,
		BaseType,
		ArrayIndexTypeId,
		FindChildren,
		DataKind,
		AddressOffset,
		Offset,
		Value,
		Count,
		ChildrenCount,
		BitPosition,
		VirtualBaseClass,
		VIRTUALTABLESHAPEID,
		VIRTUALBASEPOINTEROFFSET,
		ClassParentId,
		Nested,
		SymIndex,
		LexicalParent,
		Address,
		ThisAdjust,
		UdtKind,
		IsEquivalentTo,
		CallingConvention,
		IsCloseEquivalentTo,
		GTIEX_REQS_VALID,
		VirtualBaseOffset,
		VirtualBaseDispIndex,
		IsReference,
		IndirectVirtualBaseClass
	}

	[StructLayout(LayoutKind.Sequential)]
	struct FindChildrenParams {
		public int Count;
		public int Start;

		// hopefully no more than 256 members

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
		public int[] Child;
	}

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Variant {
        [FieldOffset(0)] public short vt;
        [FieldOffset(8)] public double dValue;
        [FieldOffset(8)] public int iValue;
        [FieldOffset(8)] public long lValue;
        [FieldOffset(8)] public byte bValue;
        [FieldOffset(8)] public short sValue;

        public override string ToString() {
            return $"VT: {vt} iValue: {iValue}";
        }
    }

    [SuppressUnmanagedCodeSecurity]
	static class Win32 {
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr OpenProcess(ProcessAccessMask access, bool inheritHandle, int pid);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern SymbolOptions SymSetOptions(SymbolOptions options);

		[DllImport("dbghelp", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SymInitializeW", ExactSpelling = true)]
		public static extern bool SymInitialize(IntPtr hProcess, string searchPath, bool invadeProcess);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymCleanup(IntPtr hProcess);

		[DllImport("dbghelp", SetLastError = true, EntryPoint = "SymLoadModuleExW", ExactSpelling = true, CharSet = CharSet.Unicode)]
		public static extern ulong SymLoadModuleEx(IntPtr hProcess, IntPtr hFile, string imageName, string moduleName, 
			ulong baseOfDll, uint dllSize, IntPtr data, uint flags);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymFromAddr(IntPtr hProcess, ulong address, out ulong displacement, ref SymbolInfo symbol);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymEnumerateModules64(IntPtr hProcess, SymEnumerateModuleProc proc, IntPtr context);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymRefreshModuleList(IntPtr hProcess);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymFromName(IntPtr hProcess, string name, ref SymbolInfo symbol);

		public delegate bool EnumSymbolCallback(ref SymbolInfo symbol, uint symbolSize, IntPtr context);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymEnumSymbols(IntPtr hProcess, ulong baseOfDll, string mask, EnumSymbolCallback callback, IntPtr context);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymEnumTypes(IntPtr hProcess, ulong baseOfDll, EnumSymbolCallback callback, IntPtr context);

        [DllImport("dbghelp", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SymEnumSourceFiles(IntPtr hProcess, ulong baseOfDll, string mask, EnumSourceFilesCallback callback, IntPtr context);

        [DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymEnumTypesByName(IntPtr hProcess, ulong baseOfDll, string mask, EnumSymbolCallback callback, IntPtr context);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymGetTypeInfo(IntPtr hProcess, ulong baseOfDll, int typeId, SymbolTypeInfo typeinfo, out int value);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymGetTypeInfo(IntPtr hProcess, ulong baseOfDll, int typeId, SymbolTypeInfo typeinfo, out SymbolTag tag);

        [DllImport("dbghelp", SetLastError = true)]
        public static extern bool SymGetTypeInfo(IntPtr hProcess, ulong baseOfDll, int typeId, SymbolTypeInfo typeinfo, out UdtKind tag);

        [DllImport("dbghelp", SetLastError = true)]
        public static extern bool SymGetTypeInfo(IntPtr hProcess, ulong baseOfDll, int typeId, SymbolTypeInfo typeinfo, out Variant value);

        [DllImport("dbghelp", SetLastError = true)]
		public unsafe static extern bool SymGetTypeInfo(IntPtr hProcess, ulong baseOfDll, int typeId, SymbolTypeInfo typeinfo, out char* value);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymGetTypeInfo(IntPtr hProcess, ulong baseOfDll, int typeId, SymbolTypeInfo typeinfo, out ulong value);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymGetTypeInfo(IntPtr hProcess, ulong baseOfDll, int typeId, SymbolTypeInfo typeinfo, ref FindChildrenParams value);

		[DllImport("dbghelp", SetLastError = true)]
		public static extern bool SymFromIndex(IntPtr hProcess, ulong dllBase, int index, ref SymbolInfo symbol);

		[DllImport("dbghelp", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SymGetTypeFromName(IntPtr hProcess, ulong dllBase, string name, ref SymbolInfo symbol);
	}
}
