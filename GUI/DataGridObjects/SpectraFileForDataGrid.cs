using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.DataGridObjects
{
    public class SpectraFileForDataGrid
    {
        public SpectraFileForDataGrid(string filePath)
        {
            FileName = Path.GetFileName(filePath);
            FilePath = filePath;
        }

        public string FileName { get; private set; }
        public string FilePath { get; private set; }
    }
}
