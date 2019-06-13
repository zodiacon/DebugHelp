using System;

namespace Zodiacon.DebugHelp {
	[Flags]
	public enum SymbolOptions : uint {
		None = 0,
		AllowAbsoluteSymbols = 0x800,
		AllowZeroAddress = 0x1000000,
		AutoPublics = 0x10000,
		CaseInsensitive = 1,
		Debug = 0x80000000,
		DeferredLoads = 0x4,
		DisableSymSrvAutoDetect = 0x2000000,
		ExactSymbols = 0x400,
		FailCriticalErrors = 0x200,
		FavorCompressed = 0x800000,
		FlatDirectory = 0x400000,
		IgnoreCodeViewRecord = 0x80,
		IgnoreImageDir = 0x200000,
		IgnoreNTSymbolPath = 0x1000,
		Include32BitModules = 0x2000,
		LoadAnything = 0x40,
		LoadLines = 0x10,
		NoCPP = 0x8,
		NoImageSearch = 0x20000,
		NoPrompts = 0x80000,
		NoPublics = 0x8000,
		NoUnqualifiedLoads = 0x100,
		Overwrite = 0x100000,
		PublicsOnly = 0x4000,
		Secure = 0x40000,
		UndecorateNames = 0x2
	}
}
