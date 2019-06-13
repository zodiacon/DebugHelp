using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Zodiacon.DebugHelp {
	static class Extensions {
		public static bool ThrowIfWin32Failed(this bool ok, int error = 0) {
			if(!ok)
				throw new Win32Exception(error != 0 ? error : Marshal.GetLastWin32Error());
			return true;
		}
	}
}
