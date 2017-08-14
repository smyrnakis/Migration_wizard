using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace foldersCompare_Verification
{
    public class CollidingFile
    {
        public FileInfo file { get; set; }

        public string PathToSource { get; set; }
        public string PathToDestination { get; set; }

        public string nodePath { get; set; }

        public int resolveAction { get; set; }
        public long sourceFileSize { get; set; }
        public long destinationFileSize { get; set; }
    }
}
