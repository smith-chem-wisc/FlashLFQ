using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.DataGridObjects
{
    public class ExperimentalDesignForDataGrid
    {
        public ExperimentalDesignForDataGrid(string filePath)
        {
            FileName = Path.GetFileName(filePath);
        }

        public string FileName { get; private set; }
        public string Condition { get; set; }
        public string Biorep { get; set; }
        public string Fraction { get; set; }
        public string Techrep { get; set; }
    }
}
