using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashLFQ
{
    class Identification
    {
        public readonly string BaseSequence;
        public readonly string FullSequence;
        public readonly double ms2RetentionTime;
        public readonly double monoisotopicMass;
        public readonly int initialChargeState;
        public double massToLookFor;
        public string fileName = "";

        public Identification(string[] input)
        {
            fileName = input[0];
            BaseSequence = input[1];
            FullSequence = input[2];
            monoisotopicMass = double.Parse(input[3]);
            ms2RetentionTime = double.Parse(input[4]);
            initialChargeState = int.Parse(input[5]);
        }
    }
}
