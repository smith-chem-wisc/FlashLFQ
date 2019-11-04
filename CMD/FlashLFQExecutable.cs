using CommandLine;
using FlashLFQ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CMD
{
    public class FlashLfqExecutable
    {
        public static List<string> acceptedSpectrumFileFormats = new List<string> { ".RAW", ".MZML" };

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<FlashLfqSettings>(args)
                   .WithParsed<FlashLfqSettings>(settings =>
                   {
                       try
                       {
                           settings.ValidateCommandLineSettings();
                       }
                       catch (Exception e)
                       {
                           if (!settings.Silent)
                           {
                               Console.WriteLine("Error: " + e.Message);
                           }
                           return;
                       }

                       // check to see if experimental design file exists
                       string assumedPathToExpDesign = Path.Combine(settings.SpectraFileRepository, "ExperimentalDesign.tsv");
                       if ((settings.Normalize || settings.BayesianFoldChangeAnalysis) && !File.Exists(assumedPathToExpDesign))
                       {
                           if (!settings.Silent)
                           {
                               Console.WriteLine("Could not find experimental design file " +
                                   "(required for normalization and Bayesian statistical analysis): " + assumedPathToExpDesign);
                           }
                           return;
                       }

                       // set up spectra file info
                       List<SpectraFileInfo> spectraFileInfos = new List<SpectraFileInfo>();
                       List<string> filePaths = Directory.GetFiles(settings.SpectraFileRepository)
                           .Where(f => acceptedSpectrumFileFormats.Contains(Path.GetExtension(f).ToUpperInvariant())).ToList();

                       // check thermo licence agreement
                       if (filePaths.Select(v => Path.GetExtension(v).ToUpper()).Any(f => f == ".RAW"))
                       {
                           var licenceAgreement = LicenceAgreementSettings.ReadLicenceSettings();

                           if (!licenceAgreement.HasAcceptedThermoLicence)
                           {
                               // decided to write this even if it's on silent mode...
                               Console.WriteLine(ThermoRawFileReader.ThermoRawFileReaderLicence.ThermoLicenceText);
                               Console.WriteLine("\nIn order to search Thermo .raw files, you must agree to the above terms. Do you agree to the above terms? y/n\n");

                               string res = Console.ReadLine().ToLowerInvariant();
                               if (res == "y")
                               {
                                   try
                                   {
                                       licenceAgreement.AcceptLicenceAndWrite();
                                   }
                                   catch (Exception e)
                                   {
                                       Console.WriteLine(e.Message);
                                   }
                               }
                               else
                               {
                                   Console.WriteLine("Thermo licence has been declined. Exiting FlashLFQ. You can still search .mzML and .mgf files without agreeing to the Thermo licence.");
                                   return;
                               }
                           }
                       }

                       if (File.Exists(assumedPathToExpDesign))
                       {
                           var experimentalDesign = File.ReadAllLines(assumedPathToExpDesign)
                               .ToDictionary(v => v.Split('\t')[0], v => v);

                           foreach (var file in filePaths)
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
                           for (int i = 0; i < filePaths.Count; i++)
                           {
                               var file = filePaths[i];
                               spectraFileInfos.Add(new SpectraFileInfo(fullFilePathWithExtension: file,
                                   condition: "Default",
                                   biorep: i,
                                   fraction: 0,
                                   techrep: 0));
                           }
                       }

                       // check the validity of the settings and experimental design
                       try
                       {
                           settings.ValidateSettings(spectraFileInfos);
                       }
                       catch (Exception e)
                       {
                           if (!settings.Silent)
                           {
                               Console.WriteLine("Error: " + e.Message);
                           }
                           return;
                       }

                       // set up IDs
                       List<Identification> ids;
                       try
                       {
                           ids = PsmReader.ReadPsms(settings.PsmIdentificationPath, settings.Silent, spectraFileInfos);
                       }
                       catch (Exception e)
                       {
                           Console.WriteLine("Problem reading PSMs: " + e.Message);
                           return;
                       }

                       if (ids.Any())
                       {
                           if (!settings.Silent)
                           {
                               Console.WriteLine("Setup is OK; read in " + ids.Count + " identifications; starting FlashLFQ engine");
                           }

                           // write FlashLFQ settings to a file
                           if (!Directory.Exists(settings.OutputPath))
                           {
                               Directory.CreateDirectory(settings.OutputPath);
                           }
                           Nett.Toml.WriteFile(settings, Path.Combine(settings.OutputPath, "FlashLfqSettings.toml"));

                           // make engine with desired settings
                           FlashLfqEngine engine = null;
                           FlashLfqResults results = null;
                           try
                           {
                               engine = new FlashLfqEngine(
                                   allIdentifications: ids,
                                   silent: settings.Silent,

                                   normalize: settings.Normalize,
                                   ppmTolerance: settings.PpmTolerance,
                                   isotopeTolerancePpm: settings.IsotopePpmTolerance,
                                   integrate: settings.Integrate,
                                   numIsotopesRequired: settings.NumIsotopesRequired,
                                   idSpecificChargeState: settings.IdSpecificCharge,

                                   matchBetweenRuns: settings.MatchBetweenRuns,
                                   matchBetweenRunsPpmTolerance: settings.PpmTolerance,
                                   maxMbrWindow: settings.MbrRtWindow,

                                   bayesianProteinQuant: settings.BayesianFoldChangeAnalysis,
                                   proteinQuantBaseCondition: settings.ControlCondition,
                                   proteinQuantFoldChangeCutoff: settings.FoldChangeCutoff,
                                   mcmcSteps: settings.McmcSteps,
                                   useSharedPeptidesForProteinQuant: settings.UseSharedPeptidesForProteinQuant,
                                   randomSeed: settings.RandomSeed
                                   );

                               // run
                               results = engine.Run();
                           }
                           catch (Exception ex)
                           {
                               string errorReportPath = Directory.GetParent(filePaths.First()).FullName;
                               if (settings.OutputPath != null)
                               {
                                   errorReportPath = settings.OutputPath;
                               }

                               if (!settings.Silent)
                               {
                                   Console.WriteLine("FlashLFQ has crashed with the following error: " + ex.Message +
                                       ".\nError report written to " + errorReportPath);
                               }

                               OutputWriter.WriteErrorReport(ex, Directory.GetParent(filePaths.First()).FullName, settings.OutputPath);
                           }

                           // output
                           if (results != null)
                           {
                               try
                               {
                                   OutputWriter.WriteOutput(settings.PsmIdentificationPath, results, settings.Silent, settings.OutputPath);
                               }
                               catch (Exception ex)
                               {
                                   if (!settings.Silent)
                                   {
                                       Console.WriteLine("Could not write FlashLFQ output: " + ex.Message);
                                   }
                               }
                           }
                       }
                       else
                       {
                           if (!settings.Silent)
                           {
                               Console.WriteLine("No peptide IDs for the specified spectra files were found! " +
                                   "Check to make sure the spectra file names match between the ID file and the spectra files");
                           }
                       }
                   })
                   .WithNotParsed<FlashLfqSettings>((errs) => HandleParseError(errs));

        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach(var error in errors)
            {
                Console.WriteLine("Error parsing argument: " + error.ToString());
            }
        }
    }
}
