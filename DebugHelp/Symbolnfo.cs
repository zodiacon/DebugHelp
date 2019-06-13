using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Zodiacon.DebugHelp {
	[Flags]
	public enum SymbolFlags : uint {
		None = 0,
		ClrToken = 0x40000,
		Constant = 0x100,
		Export = 0x200,
		Forwarder = 0x400,
		FrameRelative = 0x20,
		Function = 0x800,
		ILRelative = 0x10000,
		Local = 0x80,
		Metadata = 0x20000,
		Parameter = 0x40,
		Register = 0x8,
		RegisterRelative = 0x10,
		Slot = 0x8000,
		Thunk = 0x2000,
		TLSRelative = 0x4000,
		ValuePresent = 1,
		Virtual = 0x1000,
		Null = 0x80000,
		FunctionNoReturn = 0x100000,
		SyntheticZeroBase = 0x200000,
		PublicCode = 0x400000
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct SymbolInfo {
		// member name cannot be larger than this (docs says 2000, but seems wasteful in practice)
		const int MaxSymbolLen = 500;

		public static SymbolInfo Create() {
			var symbol = new SymbolInfo();
			symbol.Init();
			return symbol;
		}

		public void Init() {
			MaxNameLen = MaxSymbolLen;
			SizeOfStruct = 88;
		}

		public int SizeOfStruct;
		public int TypeIndex;
		readonly ulong Reserved1, Reserved2;
		public int Index;
		public int Size;
		public ulong ModuleBase;
		public SymbolFlags Flags;
		public long Value;
		public ulong Address;
		public uint Register;
		public uint Scope;
		public SymbolTag Tag;
		public uint NameLen;
		public int MaxNameLen;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxSymbolLen)]
		public string Name;
	}
}
