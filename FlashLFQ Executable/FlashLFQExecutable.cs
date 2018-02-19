using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fclp;
using FlashLFQ;

namespace FlashLFQExecutable
{
    class FlashLFQExecutable
    {
        static void Main(string[] args)
        {
            // parameters
            List<string> acceptedSpectrumFileFormats = new List<string>() { ".RAW", ".MZML" };
            
            // setup parameters
            var p = new FluentCommandLineParser<ApplicationArguments>();

            p.SetupHelp("?", "help")
             .Callback(text => Console.WriteLine(
                "Valid arguments:\n" + 
                "--idt [string|identification file path (TSV format)]\n" +
                "--rep [string|directory containing spectrum data files]\n" +
                "--out [string|output directory]\n" +
                "--ppm [double|ppm tolerance]\n" +
                "--iso [double|isotopic distribution tolerance in ppm]\n" +
                "--sil [bool|silent mode]\n" +
                "--int [bool|integrate features]\n" +
                "--mbr [bool|match between runs]\n" +
                "--mrt [double|maximum MBR window in minutes]\n" +
                "--chg [bool|use only precursor charge state]\n" +
                "--rmm [bool|require observed monoisotopic mass peak]\n" +
                "--nis [int|number of isotopes required to be observed]\n"
            ));

            p.Setup(arg => arg.psmInputPath) // PSMs file
             .As("idt").
             Required();

            p.Setup(arg => arg.rawFilesPath) // spectrum files
             .As("rep").
             Required();

            p.Setup(arg => arg.outputPath) // output path
             .As("out");

            p.Setup(arg => arg.ppmTolerance) // ppm tolerance
             .As("ppm");

            p.Setup(arg => arg.isotopePpmTolerance) // isotope ppm tolerance
             .As("iso");

            p.Setup(arg => arg.silent) // do not display output messages
             .As("sil");

            p.Setup(arg => arg.integrate) // integrate
             .As("int");

            p.Setup(arg => arg.mbr) // match between runs
             .As("mbr");

            p.Setup(arg => arg.mbrRtWindow) // maximum match-between-runs window in minutes
             .As("mrt");
            
            p.Setup(arg => arg.idSpecificChargeState) // only use PSM-identified charge states
             .As("chg");

            p.Setup(arg => arg.requireMonoisotopicMass) // require observation of monoisotopic peak
             .As("rmm");

            p.Setup(arg => arg.numIsotopesRequired) // num of isotopes required
             .As("nis");

            // args are OK - run FlashLFQ
            if (p.Parse(args).HasErrors == false && p.Object.psmInputPath != null)
            {
                // set up raw file info
                List<RawFileInfo> rawFileInfo = new List<RawFileInfo>();
                var files = Directory.GetFiles(p.Object.rawFilesPath).Where(f => acceptedSpectrumFileFormats.Contains(Path.GetExtension(f).ToUpperInvariant()));
                foreach (var file in files)
                    rawFileInfo.Add(new RawFileInfo(file));

                // set up IDs
                var ids = PsmReader.ReadPsms(p.Object.psmInputPath, p.Object.silent, rawFileInfo);

                if (ids.Any())
                {
                    if (!p.Object.silent)
                        Console.WriteLine("Setup is OK - running FlashLFQ engine");
                    // make engine with desired settings
                    FlashLFQEngine engine = new FlashLFQEngine(ids, p.Object.ppmTolerance,
                        p.Object.isotopePpmTolerance, p.Object.mbr, p.Object.mbrppmTolerance,
                        p.Object.integrate, p.Object.numIsotopesRequired, p.Object.idSpecificChargeState,
                        p.Object.requireMonoisotopicMass, p.Object.silent, null, p.Object.mbrRtWindow);

                    // run
                    var results = engine.Run();

                    // output
                    OutputWriter.WriteOutput(p.Object.psmInputPath, results, p.Object.outputPath);
                }
            }
            else if (p.Parse(args).HasErrors == false && p.Object.psmInputPath == null)
            {
                // no errors - just requesting help?
            }
            else
                Console.WriteLine("Invalid arguments - type \"--help\" for valid arguments");
        }

        internal class ApplicationArguments
        {
            #region Public Properties

            // settings
            public string psmInputPath { get; private set; }
            public string outputPath { get; private set; } = null;
            public string rawFilesPath { get; private set; }
            public double ppmTolerance { get; private set; } = 10.0;
            public double isotopePpmTolerance { get; private set; } = 5.0;
            public bool mbr { get; private set; } = false;
            public double mbrppmTolerance { get; private set; } = 5.0;
            public bool integrate { get; private set; } = false;
            public int numIsotopesRequired { get; private set; } = 2;
            public bool silent { get; private set; } = false;
            public bool idSpecificChargeState { get; private set; } = false;
            public bool requireMonoisotopicMass { get; private set; } = true;
            public double mbrRtWindow { get; private set; } = 1.5;

            #endregion Public Properties
        }
    }
}
