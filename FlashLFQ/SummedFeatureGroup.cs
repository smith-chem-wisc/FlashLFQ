using System.Text;

namespace FlashLFQ
{
    class SummedFeatureGroup
    {
        public readonly string BaseSequence;
        public readonly double[] intensitiesByFile;

        public SummedFeatureGroup(string baseSeq, double[] intensitiesByFile)
        {
            BaseSequence = baseSeq;
            this.intensitiesByFile = intensitiesByFile;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append("" + BaseSequence + '\t');
            sb.Append(string.Join("\t", intensitiesByFile));

            return sb.ToString();
        }
    }
}
