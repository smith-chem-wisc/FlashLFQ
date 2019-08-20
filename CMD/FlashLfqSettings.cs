using FlashLFQ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CMD
{
    public class FlashLfqSettings
    {
        // these are used in the command-line version only
        public string PsmIdentificationPath { get; set; }
        public string SpectraFileRepository { get; set; }
        public bool Silent { get; set; }

        // general settings
        public string OutputPath { get; set; }
        public bool Normalize { get; set; }
        public double PpmTolerance { get; set; }
        public double IsotopePpmTolerance { get; set; }
        public bool Integrate { get; set; }
        public int NumIsotopesRequired { get; set; }
        public bool IdSpecificCharge { get; set; }
        public int MaxThreads { get; set; }

        // MBR settings
        public bool MatchBetweenRuns { get; set; }
        public double MbrRtWindow { get; set; }
        public bool RequireMsMsIdentifiedPeptideInConditionForMbr { get; set; }

        // Bayesian protein quant settings
        public bool BayesianFoldChangeAnalysis { get; set; }
        public string ControlCondition { get; set; }
        public double? FoldChangeCutoff { get; set; }
        public int McmcSteps { get; set; }
        public int McmcBurninSteps { get; set; }
        public bool UseSharedPeptidesForProteinQuant { get; set; }
        public int? RandomSeed { get; set; }
        //TODO: paired samples

        public FlashLfqSettings()
        {
            FlashLfqEngine f = new FlashLfqEngine(new List<Identification>());
            var bayesianSettings = new ProteinQuantificationEngine(new FlashLfqResults(new List<SpectraFileInfo>(), new List<Identification>()), 1, "temp");

            Normalize = f.Normalize;
            PpmTolerance = f.PpmTolerance;
            IsotopePpmTolerance = f.IsotopePpmTolerance;
            Integrate = f.Integrate;
            NumIsotopesRequired = f.NumIsotopesRequired;
            IdSpecificCharge = f.IdSpecificChargeState;
            MaxThreads = f.MaxThreads;

            MatchBetweenRuns = f.MatchBetweenRuns;
            MbrRtWindow = f.MbrRtWindow;
            RequireMsMsIdentifiedPeptideInConditionForMbr = f.RequireMsmsIdInCondition;

            BayesianFoldChangeAnalysis = f.BayesianProteinQuant;
            ControlCondition = f.ProteinQuantBaseCondition;
            FoldChangeCutoff = f.ProteinQuantFoldChangeCutoff;
            McmcSteps = f.McmcSteps;
            McmcBurninSteps = f.McmcBurninSteps;
            UseSharedPeptidesForProteinQuant = f.UseSharedPeptidesForProteinQuant;

            RandomSeed = bayesianSettings.RandomSeed;
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
            if (BayesianFoldChangeAnalysis)
            {
                if (files.Select(f => f.Condition).Distinct().Count() < 2)
                {
                    throw new Exception("At least two conditions must be specified to perform the Bayesian fold-change analysis");
                }

                if (ControlCondition == null)
                {
                    throw new Exception("The control condition must be specified to perform the Bayesian fold-change analysis");
                }

                if (!files.Select(f => f.Condition).Contains(ControlCondition))
                {
                    throw new Exception("The conditions listed in the ExperimentalDesign file do not contain the specified " +
                        "control condition: " + ControlCondition);
                }

                if (McmcSteps < 500)
                {
                    throw new Exception("The number of MCMC iterations must be at least 500");
                }

                if (FoldChangeCutoff != null && FoldChangeCutoff.Value <= 0)
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
