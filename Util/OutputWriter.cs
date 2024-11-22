using System;
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

            string inputFileName = PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(inputPath);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            
            bool bayesianResults = results.ProteinGroups.Any(p => p.Value.ConditionToQuantificationResults.Any());

            //results.WritePepResults(Path.Combine(outputPath, "PEPAnalysis.txt"));
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

            string inputFileName = PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(inputPath);

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
                file.WriteLine("e: " + e);
                file.WriteLine("e.Message: " + e.Message);
                file.WriteLine("e.Data: " + e.Data);
                
                file.WriteLine("e.Source: " + e.Source);
                file.WriteLine("e.StackTrace: " + e.StackTrace);
                file.WriteLine("e.TargetSite: " + e.TargetSite);

                while (e.InnerException != null)
                {
                    e = e.InnerException;
                    file.WriteLine("e.InnerException: " + e.InnerException);
                }
            }
        }
    }
}
