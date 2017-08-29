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

            if (!engine.ReadIdentificationsFromTSV())
                return;

            engine.ConstructBinsFromIdentifications();

            Parallel.For(0, engine.filePaths.Length,
                new ParallelOptions { MaxDegreeOfParallelism = 1 },
                fileNumber =>
                {
                    if (!engine.Quantify(null, engine.filePaths[fileNumber]) && !engine.silent)
                        Console.WriteLine("Error quantifying file " + engine.filePaths[fileNumber]);
                    GC.Collect();
                }
            );

            if (engine.mbr)
                engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();

            engine.QuantifyProteins();

            if (!engine.WriteResults("_FlashLFQ_", true, true, true))
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
