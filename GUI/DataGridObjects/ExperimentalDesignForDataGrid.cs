using System.IO;

namespace GUI.DataGridObjects
{
    public class ExperimentalDesignForDataGrid
    {
        public string FileName { get; }
        public string Condition { get; set; }
        public string Biorep { get; set; }
        public string Fraction { get; set; }
        public string Techrep { get; set; }

        public ExperimentalDesignForDataGrid(string filePath)
        {
            FileName = Path.GetFileName(filePath);
        }
    }
}
