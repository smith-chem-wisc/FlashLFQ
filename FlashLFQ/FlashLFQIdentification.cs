using System.Collections.Generic;

namespace FlashLFQ
{
    public class FlashLFQIdentification
    {
        public readonly string BaseSequence;
        public readonly string FullSequence;
        public readonly double ms2RetentionTime;
        public readonly double monoisotopicMass;
        public int chargeState;
        public List<FlashLFQProteinGroup> proteinGroups;
        public double massToLookFor;
        public string fileName = "";

        public FlashLFQIdentification(string fileName, string BaseSequence, string FullSequence, double monoisotopicMass, double ms2RetentionTime, int chargeState)
        {
            this.fileName = fileName;
            this.BaseSequence = BaseSequence;
            this.FullSequence = FullSequence;
            this.monoisotopicMass = monoisotopicMass;
            this.ms2RetentionTime = ms2RetentionTime;
            this.chargeState = chargeState;
            this.proteinGroups = new List<FlashLFQProteinGroup>();
        }

        public override string ToString()
        {
            return FullSequence;
        }
    }
}
