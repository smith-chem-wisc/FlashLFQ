using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashLFQ
{
    public class FlashLFQProteinGroup
    {
        public readonly string proteinGroupName;
        public double[] intensitiesByFile;

        public FlashLFQProteinGroup(string name)
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
