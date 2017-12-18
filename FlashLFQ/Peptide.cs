using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlashLFQ
{
    public class Peptide
    {
        //public static List<RawFileInfo> rawFiles;
        public PeptideQuantities quantities;
        public readonly string Sequence;
        public readonly string ProteinGroup;
        public static List<RawFileInfo> rawFiles;
        //public readonly Dictionary<RawFileInfo, double> rawFileToIntensity;
        //public readonly Dictionary<RawFileInfo, string> rawFileToDetectionType;
        

        //public Dictionary<string, double> biorepToIntensity { get; private set; }
        //public Dictionary<string, double> techrepToIntensity { get; private set; }
        //public Dictionary<string, double> conditionToIntensity { get; private set; }
        //public Dictionary<string, double> fractionToIntensity { get; private set; }

        public Peptide(string seq, string proteinGroup)
        {
            Sequence = seq;
            ProteinGroup = proteinGroup;
            quantities = new PeptideQuantities(rawFiles);
        }

        public static string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("Sequence" + "\t");
                sb.Append("Protein Group" + "\t");
                foreach (var file in rawFiles)
                    sb.Append("Intensity_" + file.fileName + "\t");
                foreach (var file in rawFiles)
                    sb.Append("DetectionType_" + file.fileName + "\t");
                return sb.ToString();
            }
        }

        public void StashIntensities(RawFileInfo rawfile, double intensity)
        {
            var conditions = quantities.conditions.Where(p => p.conditionName.Equals(rawfile.condition));
            var bioreps = conditions.SelectMany(p => p.bioreps).Where(p => p.biorepName.Equals(rawfile.biorep));
            var fractions = bioreps.SelectMany(p => p.fractions).Where(p => p.fractionName.Equals(rawfile.fraction));
            TechnicalReplicate techrep = fractions.SelectMany(p => p.techreps).Where(p => p.techrepName.Equals(rawfile.techrep)).First();
            techrep.SetPeptideIntensity(intensity);
        }

        public void StashIntensities(RawFileInfo rawfile, double intensity, string dt)
        {
            var conditions = quantities.conditions.Where(p => p.conditionName.Equals(rawfile.condition));
            var bioreps = conditions.SelectMany(p => p.bioreps).Where(p => p.biorepName.Equals(rawfile.biorep));
            var fractions = bioreps.SelectMany(p => p.fractions).Where(p => p.fractionName.Equals(rawfile.fraction));
            TechnicalReplicate techrep = fractions.SelectMany(p => p.techreps).Where(p => p.techrepName.Equals(rawfile.techrep)).First();
            techrep.SetPeptideIntensity(intensity,dt);
        }

        public override string ToString()
        {
            var techreps = quantities.conditions.SelectMany(p => p.bioreps).SelectMany(p => p.fractions).SelectMany(p => p.techreps).OrderBy(p => p.rawFile.fileId);
            StringBuilder sb = new StringBuilder();

            sb.Append("" + Sequence + '\t');
            sb.Append("" + ProteinGroup + '\t');
            foreach (var techrep in techreps)
            {
                int fileId = techrep.rawFile.fileId;
                double intensity = techrep.intensity;
                string detectionType = techrep.detectionType;

                if (!(techrep.detectionType == "MBR" && techrep.intensity == 0))
                    sb.Append(techrep.intensity + "\t");
                else
                    sb.Append("\t");
            }

            foreach (var techrep in techreps)
            {
                if (!(techrep.detectionType == "MBR" && techrep.intensity == 0))
                    sb.Append(techrep.detectionType + "\t");
                else
                    sb.Append("\t");
            }

            return sb.ToString();
        }
    }
}
