using System;
using System.IO;
using System.Threading.Tasks;
using FlashLFQ;
using IO.MzML;
using IO.Thermo;
using MassSpectrometry;

namespace FlashLFQExecutable
{
    class FlashLFQExecutable
    {
        static void Main(string[] args)
        {
            FlashLFQEngine engine = new FlashLFQEngine();
            engine.globalStopwatch.Start();
            
            if (!engine.ParseArgs(args))
                return;

            if (!engine.ReadIdentificationsFromTSV())
                return;

            engine.ConstructIndexTemplateFromIdentifications();

            Parallel.For(0, engine.filePaths.Length,
                new ParallelOptions { MaxDegreeOfParallelism = 1 },
                fileNumber =>
                {
                    IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> dataFile;
                    if (Path.GetExtension(engine.filePaths[fileNumber]).Equals(".mzML", StringComparison.OrdinalIgnoreCase))
                        dataFile = Mzml.LoadAllStaticData(engine.filePaths[fileNumber]);
                    else
                        dataFile = ThermoDynamicData.InitiateDynamicConnection(engine.filePaths[fileNumber]);
                    if (!engine.Quantify(dataFile, engine.filePaths[fileNumber]) && !engine.silent)
                        Console.WriteLine("Error quantifying file " + engine.filePaths[fileNumber]);
                    GC.Collect();
                }
            );

            if (engine.mbr)
                engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();
            
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
