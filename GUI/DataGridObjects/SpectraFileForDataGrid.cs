using FlashLFQ;
using System.ComponentModel;
using System.IO;

namespace GUI.DataGridObjects
{
    public class SpectraFileForDataGrid
    {
        public SpectraFileForDataGrid(string filePath, string condition, int biologicalReplicate, int fraction, int technicalReplicate)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            this.Condition = condition;
            this.Sample = biologicalReplicate;
            this.Fraction = fraction;
            this.Replicate = technicalReplicate;
        }

        public string FileName { get; }
        public string FilePath { get; }
        public string Condition { get; set; }
        public int Sample { get; set; }
        public int Fraction { get; set; }
        public int Replicate { get; set; }

        public SpectraFileInfo SpectraFileInfo { get { return new SpectraFileInfo(FilePath, Condition, Sample - 1, Replicate - 1, Fraction - 1); } }
    }
}
