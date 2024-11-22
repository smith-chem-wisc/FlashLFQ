using System.IO;

namespace GUI.DataGridObjects
{
    public class IdentificationFileForDataGrid
    {
        public IdentificationFileForDataGrid(string filePath, bool? peptideFile = null)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            PeptideFile = peptideFile ?? FileName.ToLower().Contains("peptides");
        }

        public string FileName { get; }
        public string FilePath { get; }
        public bool PeptideFile { get; set; }
    }
}
