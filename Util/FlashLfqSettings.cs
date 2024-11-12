using CommandLine;
using FlashLFQ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Util
{
    public class FlashLfqSettings
    {
        // these are used in the command-line version only
        [Option("idt", Required = true, HelpText = "string; identification file path")]
        public string PsmIdentificationPath { get; set; }

        [Option("pep", Required = false, HelpText = "string; all peptide file path")]
        public string PeptideIdentificationPath { get; set; }

        [Option("rep", Required = true, HelpText = "string; directory containing spectral data files")]
        public string SpectraFileRepository { get; set; }

        [Option("sil", Default = false, HelpText = "bool; silent mode")]
        public bool Silent { get; set; }

        [Option("rea", Default = false, HelpText = "bool; filesystem is readonly, prevents writing to the FlashLFQ folder")]
        public bool ReadOnlyFileSystem { get; set; }

        [Option("pth", Default = false, HelpText = "bool; print Thermo's RawFileReader licence; required to read .raw files")]
        public bool PrintThermoLicenceViaCommandLine { get; set; }

        [Option("ath", Default = false, HelpText = "bool; accept Thermo's RawFileReader licence; required to read .raw files")]
        public bool AcceptThermoLicenceViaCommandLine { get; set; }

        // general settings
        [Option("out", Default = null, HelpText = "string; output directory")]
        public string OutputPath { get; set; }

        [Option("nor", Default = false, HelpText = "bool; normalize intensity results")]
        public bool Normalize { get; set; }

        [Option("ppm", Default = 10, HelpText = "double; ppm tolerance")]
        public double PpmTolerance { get; set; }

        [Option("iso", Default = 5, HelpText = "double; isotopic distribution tolerance in ppm")]
        public double IsotopePpmTolerance { get; set; }

        [Option("int", Default = false, HelpText = "bool; integrate peak areas (not recommended)")]
        public bool Integrate { get; set; }

        [Option("nis", Default = 2, HelpText = "int; number of isotopes required to be observed")]
        public int NumIsotopesRequired { get; set; }

        [Option("chg", Default = false, HelpText = "bool; use only precursor charge state")]
        public bool IdSpecificChargeState { get; set; }

        [Option("thr", Default = -1, HelpText = "int; number of CPU threads to use")]
        public int MaxThreads { get; set; }

        // MBR settings
        [Option("mbr", Default = true, HelpText = "bool; match between runs")]
        public bool MatchBetweenRuns { get; set; }

        [Option("mrt", Default = 1.5, HelpText = "double; maximum MBR window in minutes")]
        public double MbrRtWindow { get; set; }

        [Option("rmc", Default = false, HelpText = "bool; require MS/MS ID in condition")]
        public bool RequireMsmsIdInCondition { get; set; }

        // Bayesian protein quant settings
        [Option("bay", Default = false, HelpText = "bool; Bayesian protein fold-change analysis")]
        public bool BayesianProteinQuant { get; set; }

        [Option("ctr", Default = null, HelpText = "string; control condition for Bayesian protein fold-change analysis")]
        public string ProteinQuantBaseCondition { get; set; }

        [Option("fcc", Default = 0.1, HelpText = "double; fold-change cutoff for Bayesian protein fold-change analysis")]
        public double ProteinQuantFoldChangeCutoff { get; set; }

        [Option("mcm", Default = 3000, HelpText = "int; number of markov-chain monte carlo iterations for the Bayesian protein fold-change analysis")]
        public int McmcSteps { get; set; }

        [Option("bur", Default = 1000, HelpText = "int; number of markov-chain monte carlo burn-in iterations")]
        public int McmcBurninSteps { get; set; }

        [Option("sha", Default = false, HelpText = "bool; use shared peptides for protein quantification")]
        public bool UseSharedPeptidesForProteinQuant { get; set; }

        [Option("rns", HelpText = "int; random seed for the Bayesian protein fold-change analysis")]
        public int? RandomSeed { get; set; }
        //TODO: paired samples

        [Option("donorq", HelpText = "double; donor q value threshold")]
        public double DonorQValueThreshold  { get; set; }

        [Option("pipfdr", HelpText = "double; fdr cutoff for pip")]
        public double MbrFdrThreshold  { get; set; }

        public FlashLfqSettings()
        {
            FlashLfqEngine f = new FlashLfqEngine(new List<Identification>());
            var bayesianSettings = new ProteinQuantificationEngine(new FlashLfqResults(new List<SpectraFileInfo>(), new List<Identification>()), 1, "temp");

            Normalize = f.Normalize;
            PpmTolerance = f.PpmTolerance;
            IsotopePpmTolerance = f.IsotopePpmTolerance;
            Integrate = f.Integrate;
            NumIsotopesRequired = f.NumIsotopesRequired;
            IdSpecificChargeState = f.IdSpecificChargeState;
            MaxThreads = f.MaxThreads;

            MatchBetweenRuns = f.MatchBetweenRuns;
            MbrRtWindow = f.MbrRtWindow;
            RequireMsmsIdInCondition = f.RequireMsmsIdInCondition;
            DonorQValueThreshold = f.DonorQValueThreshold;
            MbrFdrThreshold = f.MbrDetectionQValueThreshold;

            BayesianProteinQuant = f.BayesianProteinQuant;
            ProteinQuantBaseCondition = f.ProteinQuantBaseCondition;
            ProteinQuantFoldChangeCutoff = 0.1;
            McmcSteps = f.McmcSteps;
            McmcBurninSteps = f.McmcBurninSteps;
            UseSharedPeptidesForProteinQuant = f.UseSharedPeptidesForProteinQuant;

            RandomSeed = bayesianSettings.RandomSeed;
        }

        public static FlashLfqEngine CreateEngineWithSettings(FlashLfqSettings settings, List<Identification> ids, List<string> peptidesForMbr = null)
        {
            return new FlashLfqEngine(
                allIdentifications: ids,
                silent: settings.Silent,

                normalize: settings.Normalize,
                ppmTolerance: settings.PpmTolerance,
                isotopeTolerancePpm: settings.IsotopePpmTolerance,
                integrate: settings.Integrate,
                numIsotopesRequired: settings.NumIsotopesRequired,
                idSpecificChargeState: settings.IdSpecificChargeState,
                maxThreads: settings.MaxThreads,

                matchBetweenRuns: settings.MatchBetweenRuns,
                matchBetweenRunsPpmTolerance: 10,
                maxMbrWindow: settings.MbrRtWindow,
                donorCriterion: DonorCriterion.Score,
                donorQValueThreshold: settings.DonorQValueThreshold,
                matchBetweenRunsFdrThreshold: settings.MbrFdrThreshold,
                requireMsmsIdInCondition: settings.RequireMsmsIdInCondition,

                bayesianProteinQuant: settings.BayesianProteinQuant,
                proteinQuantBaseCondition: settings.ProteinQuantBaseCondition,
                proteinQuantFoldChangeCutoff: settings.ProteinQuantFoldChangeCutoff,
                mcmcSteps: settings.McmcSteps,
                mcmcBurninSteps: settings.McmcBurninSteps,
                useSharedPeptidesForProteinQuant: settings.UseSharedPeptidesForProteinQuant,
                randomSeed: settings.RandomSeed,
                peptideSequencesToQuantify: peptidesForMbr
                );
        }

        public void ValidateCommandLineSettings()
        {
            if (PsmIdentificationPath == null)
            {
                throw new Exception("PSM identification path is required");
            }

            if (SpectraFileRepository == null)
            {
                throw new Exception("Spectra file repository is required");
            }

            if (OutputPath == null)
            {
                OutputPath = Path.GetDirectoryName(PsmIdentificationPath);
            }

            if (!File.Exists(PsmIdentificationPath))
            {
                throw new Exception("The PSM identification path does not exist: " + PsmIdentificationPath);
            }

            if (!Directory.Exists(SpectraFileRepository))
            {
                throw new Exception("The folder containing spectra files does not exist: " + SpectraFileRepository);
            }
        }

        public void ValidateSettings(List<SpectraFileInfo> files)
        {
            if (!files.Any())
            {
                throw new Exception("No spectra files were found");
            }

            // check general settings
            if (PpmTolerance <= 0)
            {
                throw new Exception("The PPM tolerance must be greater than 0");
            }

            if (IsotopePpmTolerance <= 0)
            {
                throw new Exception("The isotope PPM tolerance must be greater than 0");
            }

            if (NumIsotopesRequired < 2)
            {
                throw new Exception("The number of isotopes required must be at least 2");
            }

            if (MbrRtWindow <= 0)
            {
                throw new Exception("The match-between-runs time window must be greater than 0");
            }

            // check bayesian stats parameters
            if (BayesianProteinQuant)
            {
                if (files.Select(f => f.Condition).Distinct().Count() < 2)
                {
                    throw new Exception("At least two conditions must be specified to perform the Bayesian fold-change analysis");
                }

                if (ProteinQuantBaseCondition == null)
                {
                    throw new Exception("The control condition must be specified to perform the Bayesian fold-change analysis");
                }

                if (!files.Select(f => f.Condition).Contains(ProteinQuantBaseCondition))
                {
                    throw new Exception("The conditions listed in the ExperimentalDesign file do not contain the specified " +
                        "control condition: " + ProteinQuantBaseCondition);
                }

                if (McmcSteps < 500)
                {
                    throw new Exception("The number of MCMC iterations must be at least 500");
                }

                if (ProteinQuantFoldChangeCutoff < 0)
                {
                    throw new Exception("The fold-change cutoff must be greater than zero");
                }
            }

            // check experimental design
            ValidateExperimentalDesign(files);
        }

        public void ValidateExperimentalDesign(List<SpectraFileInfo> files)
        {
            // this is a little weird because the SpectraFileInfo object's sample/fraction/replicate
            // numbers are zero-based but in the GUI and text files they're one-based

            // check for basic parsing
            foreach (SpectraFileInfo file in files)
            {
                if (string.IsNullOrWhiteSpace(file.Condition))
                {
                    throw new Exception(
                        "Condition: " + file.Condition +
                        "\nSample: " + file.BiologicalReplicate +
                        "\nFraction: " + file.Fraction +
                        "\nReplicate: " + file.TechnicalReplicate +
                        "\n\nCondition cannot be blank");
                }

                if (file.BiologicalReplicate < 0)
                {
                    throw new Exception(
                        "Condition: " + file.Condition +
                        "\nSample: " + file.BiologicalReplicate +
                        "\nFraction: " + file.Fraction +
                        "\nReplicate: " + file.TechnicalReplicate +
                        "\n\nSample must be an integer >= 1");
                }

                if (file.Fraction < 0)
                {
                    throw new Exception(
                        "Condition: " + file.Condition +
                        "\nSample: " + file.BiologicalReplicate +
                        "\nFraction: " + file.Fraction +
                        "\nReplicate: " + file.TechnicalReplicate +
                        "\n\nFraction must be an integer >= 1");
                }

                if (file.TechnicalReplicate < 0)
                {
                    throw new Exception(
                        "Condition: " + file.Condition +
                        "\nSample: " + file.BiologicalReplicate +
                        "\nFraction: " + file.Fraction +
                        "\nReplicate: " + file.TechnicalReplicate +
                        "\n\nReplicate must be an integer >= 1");
                }
            }

            // check for correct iteration of integer values and duplicates
            var conditions = files.GroupBy(p => p.Condition);

            foreach (var condition in conditions)
            {
                var temp = condition.OrderBy(p => p.BiologicalReplicate).ThenBy(p => p.Fraction).ThenBy(p => p.TechnicalReplicate);
                int numB = temp.Max(p => p.BiologicalReplicate) + 1;

                // check bioreps are in order
                for (int b = 0; b < numB; b++)
                {
                    var biorepFiles = temp.Where(p => p.BiologicalReplicate == b);

                    if (!biorepFiles.Any())
                    {
                        throw new Exception("Condition \"" + condition.Key + "\" sample " + (b + 1) + " is missing!");
                    }

                    // check fractions are in order
                    int numF = biorepFiles.Max(p => p.Fraction) + 1;

                    for (int f = 0; f < numF; f++)
                    {
                        var fractionFiles = biorepFiles.Where(p => p.Fraction == f);

                        if (!fractionFiles.Any())
                        {
                            throw new Exception("Condition \"" + condition.Key + "\" sample " + (b + 1) + " fraction " + (f + 1) + " is missing!");
                        }

                        // check techreps are in order
                        int numT = fractionFiles.Max(p => p.TechnicalReplicate) + 1;

                        for (int t = 0; t < numT; t++)
                        {
                            var techrepFiles = fractionFiles.Where(p => p.TechnicalReplicate == t);

                            if (!techrepFiles.Any())
                            {
                                throw new Exception("Condition \"" + condition.Key + "\" sample " + (b + 1) +
                                    " fraction " + (f + 1) + " replicate " + (t + 1) + " is missing!");
                            }

                            if (techrepFiles.Count() > 1)
                            {
                                throw new Exception("Duplicates are not allowed:\n" +
                                    "Condition \"" + condition.Key + "\" sample " + (b + 1) +
                                    " fraction " + (f + 1) + " replicate " + (t + 1));
                            }
                        }
                    }
                }
            }
        }

        //TODO
        public void ValidateIdentificationFile()
        {

        }
    }
}
