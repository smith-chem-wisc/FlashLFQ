using System.IO;

namespace GUI.DataGridObjects
{
    public class IdentificationFileForDataGrid
    {
        public IdentificationFileForDataGrid(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
        }

        public string FileName { get; }
        public string FilePath { get; }
    }
}
