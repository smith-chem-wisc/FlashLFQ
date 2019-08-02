using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fclp;
using FlashLFQ;

namespace CMD
{
    public class FlashLfqExecutable
    {
        public static void Main(string[] args)
        {
            // parameters
            List<string> acceptedSpectrumFileFormats = new List<string> { ".RAW", ".MZML" };

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
                "--nis [int|number of isotopes required to be observed]\n" +
                "--nor [bool|normalize intensity results]\n" +
                "--pro [bool|advanced protein quantification]\n"
            ));

            p.Setup(arg => arg.PsmInputPath) // PSMs file
             .As("idt").
             Required();

            p.Setup(arg => arg.RawFilesPath) // spectrum files
             .As("rep").
             Required();

            p.Setup(arg => arg.OutputPath) // output path
             .As("out");

            p.Setup(arg => arg.PpmTolerance) // ppm tolerance
             .As("ppm");

            p.Setup(arg => arg.IsotopePpmTolerance) // isotope ppm tolerance
             .As("iso");

            p.Setup(arg => arg.Silent) // do not display output messages
             .As("sil");

            p.Setup(arg => arg.Integrate) // integrate
             .As("int");

            p.Setup(arg => arg.MatchBetweenRuns) // match between runs
             .As("mbr");

            p.Setup(arg => arg.MbrRtWindow) // maximum match-between-runs window in minutes
             .As("mrt");

            p.Setup(arg => arg.IdSpecificChargeState) // only use PSM-identified charge states
             .As("chg");

            p.Setup(arg => arg.RequireMonoisotopicMass) // require observation of monoisotopic peak
             .As("rmm");

            p.Setup(arg => arg.NumIsotopesRequired) // num of isotopes required
             .As("nis");

            p.Setup(arg => arg.Normalize) // normalize
             .As("nor");

            p.Setup(arg => arg.AdvancedProteinQuant) // advanced protein quant
             .As("pro");

            // args are OK - run FlashLFQ
            if (!p.Parse(args).HasErrors && p.Object.PsmInputPath != null)
            {
                if (!File.Exists(p.Object.PsmInputPath))
                {
                    if (!p.Object.Silent)
                    {
                        Console.WriteLine("Could not locate identification file " + p.Object.PsmInputPath);
                    }
                    return;
                }

                if (!Directory.Exists(p.Object.RawFilesPath))
                {
                    if (!p.Object.Silent)
                    {
                        Console.WriteLine("Could not locate folder " + p.Object.RawFilesPath);
                    }
                    return;
                }

                string assumedPathToExpDesign = Path.Combine(p.Object.RawFilesPath, "ExperimentalDesign.tsv");
                if (p.Object.Normalize && !File.Exists(assumedPathToExpDesign))
                {
                    if (!p.Object.Silent)
                    {
                        Console.WriteLine("Could not find experimental design file (required for normalization): " + assumedPathToExpDesign);
                    }
                    return;
                }

                // set up spectra file info
                List<SpectraFileInfo> spectraFileInfos = new List<SpectraFileInfo>();
                IEnumerable<string> files = Directory.GetFiles(p.Object.RawFilesPath)
                    .Where(f => acceptedSpectrumFileFormats.Contains(Path.GetExtension(f).ToUpperInvariant()));

                if (File.Exists(assumedPathToExpDesign))
                {
                    var experimentalDesign = File.ReadAllLines(assumedPathToExpDesign)
                        .ToDictionary(v => v.Split('\t')[0], v => v);

                    foreach (var file in files)
                    {
                        string filename = Path.GetFileNameWithoutExtension(file);

                        var expDesignForThisFile = experimentalDesign[filename];
                        var split = expDesignForThisFile.Split('\t');

                        string condition = split[1];
                        int biorep = int.Parse(split[2]);
                        int fraction = int.Parse(split[3]);
                        int techrep = int.Parse(split[4]);

                        // experimental design info passed in here for each spectra file
                        spectraFileInfos.Add(new SpectraFileInfo(fullFilePathWithExtension: file,
                            condition: condition,
                            biorep: biorep - 1,
                            fraction: fraction - 1,
                            techrep: techrep - 1));
                    }
                }
                else
                {
                    foreach (var file in files)
                    {
                        spectraFileInfos.Add(new SpectraFileInfo(fullFilePathWithExtension: file,
                            condition: "",
                            biorep: 0,
                            fraction: 0,
                            techrep: 0));
                    }
                }

                // set up IDs
                List<Identification> ids;
                try
                {
                    ids = PsmReader.ReadPsms(p.Object.PsmInputPath, p.Object.Silent, spectraFileInfos);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Problem reading PSMs: " + e.Message);
                    return;
                }

                if (ids.Any())
                {
                    if (!p.Object.Silent)
                    {
                        Console.WriteLine("Setup is OK; read in " + ids.Count + " identifications; starting FlashLFQ engine");
                    }

                    // make engine with desired settings
                    FlashLfqEngine engine = null;
                    FlashLfqResults results = null;
                    try
                    {
                        engine = new FlashLfqEngine(
                            allIdentifications: ids,
                            normalize: p.Object.Normalize,
                            ppmTolerance: p.Object.PpmTolerance,
                            isotopeTolerancePpm: p.Object.IsotopePpmTolerance,
                            matchBetweenRuns: p.Object.MatchBetweenRuns,
                            matchBetweenRunsPpmTolerance: p.Object.MbrPpmTolerance,
                            integrate: p.Object.Integrate,
                            numIsotopesRequired: p.Object.NumIsotopesRequired,
                            idSpecificChargeState: p.Object.IdSpecificChargeState,
                            requireMonoisotopicMass: p.Object.RequireMonoisotopicMass,
                            silent: p.Object.Silent,
                            optionalPeriodicTablePath: null,
                            maxMbrWindow: p.Object.MbrRtWindow,
                            advancedProteinQuant: p.Object.AdvancedProteinQuant);

                        // run
                        results = engine.Run();
                    }
                    catch (Exception ex)
                    {
                        string errorReportPath = Directory.GetParent(files.First()).FullName;
                        if (p.Object.OutputPath != null)
                        {
                            errorReportPath = p.Object.OutputPath;
                        }

                        if (!p.Object.Silent)
                        {
                            Console.WriteLine("FlashLFQ has crashed with the following error: " + ex.Message +
                                ".\nError report written to " + errorReportPath);
                        }

                        OutputWriter.WriteErrorReport(ex, Directory.GetParent(files.First()).FullName, p.Object.OutputPath);
                    }

                    // output
                    if (results != null)
                    {
                        if (!p.Object.Silent)
                        {
                            Console.WriteLine("Writing output...");
                        }

                        try
                        {
                            OutputWriter.WriteOutput(p.Object.PsmInputPath, results, p.Object.OutputPath);

                            if (!p.Object.Silent)
                            {
                                Console.WriteLine("Finished writing output");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!p.Object.Silent)
                            {
                                Console.WriteLine("Could not write FlashLFQ output: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    if (!p.Object.Silent)
                    {
                        Console.WriteLine("No peptide IDs for the specified spectra files were found! " +
                            "Check to make sure the spectra file names match between the ID file and the spectra files");
                    }
                }
            }
            else if (p.Parse(args).HasErrors == false && p.Object.PsmInputPath == null)
            {
                // no errors - just requesting help?
            }
            else
            {
                Console.WriteLine("Invalid arguments - type \"--help\" for valid arguments");
            }
        }

        internal class ApplicationArguments
        {
            // settings
            public string PsmInputPath { get; private set; } = null;
            public string OutputPath { get; private set; } = null;
            public string RawFilesPath { get; private set; } = null;
            public double PpmTolerance { get; private set; } = 10.0;
            public double IsotopePpmTolerance { get; private set; } = 5.0;
            public bool MatchBetweenRuns { get; private set; } = false;
            public double MbrPpmTolerance { get; private set; } = 5.0;
            public bool Integrate { get; private set; } = false;
            public int NumIsotopesRequired { get; private set; } = 2;
            public bool Silent { get; private set; } = false;
            public bool IdSpecificChargeState { get; private set; } = false;
            public bool RequireMonoisotopicMass { get; private set; } = true;
            public double MbrRtWindow { get; private set; } = 1.5;
            public bool Normalize { get; private set; } = false;
            public bool AdvancedProteinQuant { get; private set; } = false;
        }
    }
}
