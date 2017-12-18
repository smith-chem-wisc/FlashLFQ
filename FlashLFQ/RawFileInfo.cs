using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashLFQ
{
    public class RawFileInfo
    {
        private static int nextGlobalFileId = 0; // fileId automatically generated in the constructor, do not touch this

        public string fileName;
        public string fullFilePath;
        public string condition;
        public string biorep;
        public string fraction;
        public string techrep;
        public readonly int fileId;
        public string analysisSummary;
        public List<Identification> IdentificationsForThisFile;
        public List<ChromatographicPeak> peaksForThisFile;

        public RawFileInfo(string fileName, string c, string br, string f, string t)
        {
            this.fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            this.condition = c;
            this.biorep = br;
            this.fraction = f;
            this.techrep = t;
            peaksForThisFile = new List<ChromatographicPeak>();
            IdentificationsForThisFile = new List<Identification>();

            this.fileId = nextGlobalFileId;
            nextGlobalFileId++;
        }

        public RawFileInfo(string fileName)
        {
            this.fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);


            this.condition = "default";
            this.biorep = "default";
            this.fraction = "default";
            this.techrep = "default";




            peaksForThisFile = new List<ChromatographicPeak>();
            IdentificationsForThisFile = new List<Identification>();

            this.fileId = nextGlobalFileId;
            nextGlobalFileId++;
        }
    }
}
