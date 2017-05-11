using System;
using UsefulProteomicsDatabases;

namespace FlashLFQ
{
    class Driver
    {
        static void Main(string[] args)
        {
            FlashLfqEngine engine = new FlashLfqEngine();
            
            try
            {
                Loaders.LoadElements(".\\elements.dat");
            }
            catch (Exception e)
            {
                if (!engine.silent)
                {
                    Console.WriteLine("\nCan't read periodic table file\n");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                }
                return;
            }

            if (!engine.ParseArgs(args))
                return;

            if (!engine.ReadIdentificationsFromTSV())
                return;

            engine.ConstructBins();

            // main file quantification loop
            for (int i = 0; i < engine.massSpecFilePaths.Length; i++)
            {
                if (!engine.ReadMSFile(i))
                    return;
                engine.FillBins();
                engine.Quantify(i);
                //engine.EmptyBins();
                //engine.CloseRawFile();
            }

            if (!engine.WriteResults())
                return;

            if (!engine.silent)
            {
                Console.WriteLine("Done");
            }

            if(engine.pause)
                Console.ReadKey();
        }
    }
}
