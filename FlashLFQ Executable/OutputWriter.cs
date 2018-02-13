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
            if(outputPath == null)
                outputPath = Path.GetDirectoryName(inputPath);
            string inputFileName = Path.GetFileNameWithoutExtension(inputPath);

            // peak output
            List<string> output = new List<string>() { ChromatographicPeak.TabSeparatedHeader };
            foreach (var peak in results.peaks.SelectMany(p => p.Value).OrderBy(p => p.rawFileInfo.filenameWithoutExtension))
                output.Add(peak.ToString());
            File.WriteAllLines(outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedPeaks.tsv", output);

            // peptide base sequence output
            output = new List<string>() { Peptide.TabSeparatedHeader };
            foreach (var pep in results.peptideBaseSequences.OrderBy(p => p.Value.Sequence))
                output.Add(pep.Value.ToString());
            File.WriteAllLines(outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedBaseSequences.tsv", output);

            // peptide mod sequence output
            output = new List<string>() { Peptide.TabSeparatedHeader };
            foreach (var pep in results.peptideModifiedSequences.OrderBy(p => p.Value.Sequence))
                output.Add(pep.Value.ToString());
            File.WriteAllLines(outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedModifiedSequences.tsv", output);

            // protein output
            output = new List<string>() { ProteinGroup.TabSeparatedHeader };
            foreach (var protein in results.proteinGroups.OrderBy(p => p.Value.ProteinGroupName))
                output.Add(protein.Value.ToString());
            File.WriteAllLines(outputPath + Path.DirectorySeparatorChar + inputFileName + "_FlashLFQ_QuantifiedProteins.tsv", output);
        }
    }
}
