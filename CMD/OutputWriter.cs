using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlashLFQ;
using MzLibUtil;

namespace CMD
{
    public class OutputWriter
    {
        public static void WriteOutput(string inputPath, FlashLfqResults results, string outputPath = null)
        {
            if (outputPath == null)
            {
                outputPath = Path.GetDirectoryName(inputPath);
            }

            string inputFileName = Path.GetFileNameWithoutExtension(inputPath);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string append = "_FlashLFQ_";
            if (inputFileName.ToLowerInvariant().Contains("flashlfq"))
            {
                append = "_";
            }

            results.WriteResults(
                outputPath + Path.DirectorySeparatorChar + inputFileName + append + "QuantifiedPeaks.tsv",
                outputPath + Path.DirectorySeparatorChar + inputFileName + append + "QuantifiedModifiedSequences.tsv",
                outputPath + Path.DirectorySeparatorChar + inputFileName + append + "QuantifiedBaseSequences.tsv",
                outputPath + Path.DirectorySeparatorChar + inputFileName + append + "QuantifiedProteins.tsv"
                );
        }

        public static void WriteErrorReport(Exception e, string inputPath, string outputPath = null)
        {
            if (outputPath == null)
            {
                outputPath = Path.GetDirectoryName(inputPath);
            }

            string inputFileName = Path.GetFileNameWithoutExtension(inputPath);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var resultsFileName = Path.Combine(outputPath, "ErrorReport.txt");
            e.Data.Add("folder", outputPath);

            using (StreamWriter file = new StreamWriter(resultsFileName))
            {
                file.WriteLine(SystemInfo.CompleteSystemInfo()); //OS, OS Version, .Net Version, RAM, processor count, MSFileReader .dll versions X3
                file.Write("e: " + e);
                file.Write("e.Message: " + e.Message);
                file.Write("e.InnerException: " + e.InnerException);
                file.Write("e.Source: " + e.Source);
                file.Write("e.StackTrace: " + e.StackTrace);
                file.Write("e.TargetSite: " + e.TargetSite);
            }
        }
    }
}
