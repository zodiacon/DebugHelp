using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zodiacon.DebugHelp {
    public class SourceFile {
        public ulong BaseAddress { get; set; }
        public string FileName { get; set; }
    }

    public class SourceFileLine {
        public SourceFile SourceFile { get; }
        public string Line { get; }

        public SourceFileLine(SourceFile file, string line) {
            SourceFile = file;
            Line = line;
        }
    }

}
