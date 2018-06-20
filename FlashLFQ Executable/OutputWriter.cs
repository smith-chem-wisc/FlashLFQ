using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlashLFQ;

namespace FlashLFQExecutable
{
    public class OutputWriter
    {
        public static void WriteOutput(string inputPath, FlashLFQResults results, string outputPath = null)
        {
            if (outputPath == null)
            {
                outputPath = Path.GetDirectoryName(inputPath);
            }

            string inputFileName = Path.GetFileNameWithoutExtension(inputPath);

            if(!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            results.WriteResults(
                outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedPeaks.tsv",
                outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedModifiedSequences.tsv",
                outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedBaseSequences.tsv",
                outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedProteins.tsv"
                );
        }
    }
}
