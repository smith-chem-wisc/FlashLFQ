using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlashLFQ;
using MzLibUtil;

namespace Util
{
    public class OutputWriter
    {
        public static void WriteOutput(string inputPath, FlashLfqResults results, bool silent, string outputPath = null)
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
            
            bool bayesianResults = results.ProteinGroups.Any(p => p.Value.ConditionToQuantificationResults.Any());

            results.WriteResults(
                Path.Combine(outputPath, "QuantifiedPeaks.tsv"),
                Path.Combine(outputPath, "QuantifiedPeptides.tsv"),
                Path.Combine(outputPath, "QuantifiedProteins.tsv"),
                bayesianResults ? Path.Combine(outputPath, "BayesianFoldChangeAnalysis.tsv") : null,
                silent
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
                file.WriteLine("FlashLFQ version: " + typeof(OutputWriter).Assembly.GetName().Version.ToString());
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
