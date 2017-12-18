using System;
using System.Threading.Tasks;
using FlashLFQ;

namespace FlashLFQExecutable
{
    class FlashLFQExecutable
    {
        static void Main(string[] args)
        {
            FlashLFQEngine engine = new FlashLFQEngine();
            engine.globalStopwatch.Start();

            if (!engine.ReadPeriodicTable(null))
                return;

            if (!engine.ParseArgs(args))
                return;
            if (!engine.ReadCBFTKey())
                return;
            if (!engine.ReadIdentificationsFromTSV())
                return;

            engine.ConstructIndexTemplateFromIdentifications();

            Parallel.For(0, engine.rawFileInfos.Count,
                new ParallelOptions { MaxDegreeOfParallelism = 1 },
                fileNumber =>
                {
                    if (!engine.Quantify(null, engine.rawFileInfos[fileNumber].fullFilePath) && !engine.silent)
                        Console.WriteLine("Error quantifying file " + engine.rawFileInfos[fileNumber].fullFilePath);
                    GC.Collect();
                }
            );

            if (engine.mbr)
                engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();



            if (!engine.WriteResults("_FlashLFQ_", true, true, true))
                return;

            if (!engine.normalize)
                return;

            if (!engine.WriteNormalizedResults("_FlashLFQ_Normalized", true))
                return;
            if (!engine.WriteNormalizedResults("_FlashLFQ_Normalized", false))
                return;

            if (!engine.silent)
                Console.WriteLine("All done");
            
            if (!engine.silent)
                Console.WriteLine("Analysis time: " + engine.globalStopwatch.Elapsed.Hours + "h " + engine.globalStopwatch.Elapsed.Minutes + "m " + 
                    engine.globalStopwatch.Elapsed.Seconds + "s");
            
            if (engine.pause)
                Console.ReadKey();
        }
    }
}
