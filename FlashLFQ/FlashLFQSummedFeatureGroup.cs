using System.Text;

namespace FlashLFQ
{
    public class FlashLFQSummedFeatureGroup
    {
        public readonly string BaseSequence;
        public readonly double[] intensitiesByFile;
        public readonly string[] detectionType;
        public static string[] files;

        public FlashLFQSummedFeatureGroup(string baseSeq, double[] intensitiesByFile, string[] detectionType, string[] files)
        {
            BaseSequence = baseSeq;
            this.intensitiesByFile = intensitiesByFile;
            this.detectionType = detectionType;
            FlashLFQSummedFeatureGroup.files = files;
        }

        public static string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("Base Sequence" + "\t");
                for(int i = 0; i < files.Length; i++)
                    sb.Append("Intensity_" + files[i] + "\t");
                for (int i = 0; i < files.Length; i++)
                    sb.Append("Detection Type_" + files[i] + "\t");
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append("" + BaseSequence + '\t');
            sb.Append(string.Join("\t", intensitiesByFile) + '\t');
            sb.Append(string.Join("\t", detectionType));

            return sb.ToString();
        }
    }
}
