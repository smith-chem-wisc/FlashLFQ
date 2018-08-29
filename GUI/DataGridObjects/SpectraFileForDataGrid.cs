using System.IO;

namespace GUI.DataGridObjects
{
    public class SpectraFileForDataGrid
    {
        public string FileName { get; }
        public string FilePath { get; }

        public SpectraFileForDataGrid(string filePath)
        {
            FileName = Path.GetFileName(filePath);
            FilePath = filePath;
        }
    }
}
