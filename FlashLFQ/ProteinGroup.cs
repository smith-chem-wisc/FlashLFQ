using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashLFQ
{
    class ProteinGroup
    {
        public readonly string proteinGroupName;
        public double[] intensitiesByFile;

        public ProteinGroup(string name)
        {
            proteinGroupName = name;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("" + proteinGroupName + '\t');
            sb.Append(string.Join("\t", intensitiesByFile));

            return sb.ToString();
        }
    }
}
