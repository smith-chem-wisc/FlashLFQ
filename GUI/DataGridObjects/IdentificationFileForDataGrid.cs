using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.DataGridObjects
{
    public class IdentificationFileForDataGrid
    {
        public IdentificationFileForDataGrid(string filePath)
        {
            this.FilePath = filePath;
            FileName = Path.GetFileName(filePath);
        }

        public string FileName { get; private set; }
        public string FilePath { get; private set; }
    }
}
