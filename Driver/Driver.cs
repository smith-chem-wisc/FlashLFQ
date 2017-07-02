using System;
using System.Threading.Tasks;

namespace FlashLFQ
{
    class Driver
    {
        static void Main(string[] args)
        {
            FlashLFQEngine engine = new FlashLFQEngine();
            engine.stopwatch.Start();

            if (!engine.ReadPeriodicTable())
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
                    engine.Quantify(null, engine.filePaths[fileNumber]);
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
                Console.WriteLine("Analysis time: " + engine.stopwatch.Elapsed.Hours + "h " + engine.stopwatch.Elapsed.Minutes + "m " + 
                    engine.stopwatch.Elapsed.Seconds + "s");


            if (engine.pause)
                Console.ReadKey();
        }
    }
}
