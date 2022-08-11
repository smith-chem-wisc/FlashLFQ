using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class ScanHeaderInfo
    {
        public ScanHeaderInfo(string fullFilePathWithExtension, string filename, int scanNumber, double retentionTime)
        {
            FullFilePathWithExtension = fullFilePathWithExtension;
            FileNameWithoutExtension = filename;
            ScanNumber = scanNumber;
            RetentionTime = retentionTime;
        }
        public string FullFilePathWithExtension { get; private set; }
        public string FileNameWithoutExtension { get; private set; }
        public int ScanNumber { get; private set; }
        public double RetentionTime { get; private set; }
    }
}
