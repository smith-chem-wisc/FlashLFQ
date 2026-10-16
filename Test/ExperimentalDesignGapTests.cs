using FlashLFQ;
using MassSpectrometry;
using NUnit.Framework;
using Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Util;

namespace Test
{
    /// <summary>
    /// A gap in an experimental design's numbering (a condition's bioreps, a biorep's fractions, a fraction's
    /// techreps) is quantified as numbered and warned about; only a duplicate is refused. MetaMorpheus's
    /// ExperimentalDesign follows the same rule in the same words, and mzLib's SdrfLabelFreeDesign follows it too.
    /// </summary>
    [TestFixture]
    internal class ExperimentalDesignGapTests
    {
        // SpectraFileInfo numbers are zero-based; the design file and these messages are one-based
        private static SpectraFileInfo Row(string name, string condition, int biorep, int fraction, int techrep)
        {
            return new SpectraFileInfo(name + ".mzML", condition, biorep - 1, techrep - 1, fraction - 1);
        }

        [Test]
        public static void ABiorepGapIsAWarningNotAnError()
        {
            var files = new List<SpectraFileInfo>
            {
                Row("a1", "A", 1, 1, 1),
                Row("a2", "A", 2, 1, 1),
                Row("a4", "A", 4, 1, 1),
            };

            Assert.DoesNotThrow(() => new FlashLfqSettings().ValidateExperimentalDesign(files));
            Assert.That(FlashLfqSettings.GetWarningsInExperimentalDesign(files), Is.EqualTo(new[]
            {
                "Condition \"A\": biological replicates 1, 2, 4, quantified as numbered; biological replicate 3 is not in the design. " +
                "A missing number may be a sample or file that was lost or not searched."
            }));
        }

        [Test]
        public static void StudyWideBiorepNumbersWarnOnlyForTheConditionThatDoesNotStartAtOne()
        {
            var files = new List<SpectraFileInfo>
            {
                Row("c1", "control", 1, 1, 1),
                Row("c2", "control", 2, 1, 1),
                Row("t3", "treated", 3, 1, 1),
                Row("t4", "treated", 4, 1, 1),
            };

            Assert.DoesNotThrow(() => new FlashLfqSettings().ValidateExperimentalDesign(files));
            Assert.That(FlashLfqSettings.GetWarningsInExperimentalDesign(files), Is.EqualTo(new[]
            {
                "Condition \"treated\": biological replicates 3, 4, quantified as numbered; biological replicates 1, 2 are not in the design. " +
                "A missing number may be a sample or file that was lost or not searched."
            }));
        }

        [Test]
        public static void FractionAndTechrepGapsAreWarningsNamingTheirBiorepAndFraction()
        {
            var files = new List<SpectraFileInfo>
            {
                Row("f1", "A", 1, 1, 1),
                Row("f3", "A", 1, 3, 1),
                Row("f3t3", "A", 1, 3, 3),
            };

            Assert.DoesNotThrow(() => new FlashLfqSettings().ValidateExperimentalDesign(files));
            Assert.That(FlashLfqSettings.GetWarningsInExperimentalDesign(files), Is.EqualTo(new[]
            {
                "Condition \"A\" biorep 1: fractions 1, 3, quantified as numbered; fraction 2 is not in the design. " +
                "A missing number may be a sample or file that was lost or not searched.",
                "Condition \"A\" biorep 1 fraction 3: technical replicates 1, 3, quantified as numbered; technical replicate 2 is not in the design. " +
                "A missing number may be a sample or file that was lost or not searched.",
            }));
        }

        [Test]
        public static void ContiguousNumbersGiveNoWarning()
        {
            var files = new List<SpectraFileInfo>
            {
                Row("a1", "A", 1, 1, 1),
                Row("a1f2", "A", 1, 2, 1),
                Row("a1f2t2", "A", 1, 2, 2),
                Row("a2", "A", 2, 1, 1),
                Row("b1", "B", 1, 1, 1),
            };

            Assert.DoesNotThrow(() => new FlashLfqSettings().ValidateExperimentalDesign(files));
            Assert.That(FlashLfqSettings.GetWarningsInExperimentalDesign(files), Is.Empty);
        }

        [Test]
        public static void ADuplicateIsStillRefused()
        {
            var files = new List<SpectraFileInfo>
            {
                Row("a1", "A", 1, 1, 1),
                Row("a3", "A", 3, 2, 1),
                Row("a3again", "A", 3, 2, 1),
            };

            var ex = Assert.Throws<Exception>(() => new FlashLfqSettings().ValidateExperimentalDesign(files));
            Assert.That(ex.Message, Is.EqualTo("Duplicates are not allowed:\nCondition \"A\" biorep 3 fraction 2 techrep 1"));
        }

        /// <summary>
        /// The command line quantifies a gapped design, prints the warning, and gives the same normalized
        /// peptide intensities as the same files numbered without the gap. Each file is a copy of one mzML with
        /// its intensities scaled, so normalization has a difference to remove: the un-normalized run shows that
        /// it changes this fixture, so the comparison is not trivially equal. The biorep case lacks biorep 1,
        /// which needs mzLib #1422: before it, biorep normalization takes biorep 1 as its reference and silently
        /// does nothing when it is missing.
        /// </summary>
        [Test]
        public static void TheCommandLineQuantifiesABiorepGapAsNumberedAndSaysSo()
        {
            AssertGapIsQuantifiedAsNumbered("biorep",
                new[] { ("a", "Default", 2, 1, 1, 1.0), ("b", "Default", 3, 1, 1, 2.0) },
                new[] { ("a", "Default", 1, 1, 1, 1.0), ("b", "Default", 2, 1, 1, 2.0) },
                "Condition \"Default\": biological replicates 2, 3, quantified as numbered; biological replicate 1 is not in the design.");
        }

        [Test]
        public static void TheCommandLineQuantifiesAFractionGapAsNumberedAndSaysSo()
        {
            AssertGapIsQuantifiedAsNumbered("fraction",
                new[] { ("a", "Default", 1, 1, 1, 1.0), ("b", "Default", 1, 3, 1, 1.0), ("c", "Default", 2, 1, 1, 2.0), ("d", "Default", 2, 3, 1, 3.0) },
                new[] { ("a", "Default", 1, 1, 1, 1.0), ("b", "Default", 1, 2, 1, 1.0), ("c", "Default", 2, 1, 1, 2.0), ("d", "Default", 2, 2, 1, 3.0) },
                "Condition \"Default\" biorep 1: fractions 1, 3, quantified as numbered; fraction 2 is not in the design.");
        }

        [Test]
        public static void TheCommandLineQuantifiesATechrepGapAsNumberedAndSaysSo()
        {
            AssertGapIsQuantifiedAsNumbered("techrep",
                new[] { ("a", "Default", 1, 1, 1, 1.0), ("b", "Default", 1, 1, 3, 2.0) },
                new[] { ("a", "Default", 1, 1, 1, 1.0), ("b", "Default", 1, 1, 2, 2.0) },
                "Condition \"Default\" biorep 1 fraction 1: technical replicates 1, 3, quantified as numbered; technical replicate 2 is not in the design.");
        }

        private static void AssertGapIsQuantifiedAsNumbered(string level,
            (string Name, string Condition, int Biorep, int Fraction, int Techrep, double Scale)[] gapped,
            (string Name, string Condition, int Biorep, int Fraction, int Techrep, double Scale)[] contiguous,
            string expectedWarning)
        {
            (string gappedOutput, string gappedPeptides) = RunDesign("Gap_" + level + "_gapped", gapped, normalize: true);
            (string contiguousOutput, string contiguousPeptides) = RunDesign("Gap_" + level + "_contiguous", contiguous, normalize: true);
            (_, string unnormalizedPeptides) = RunDesign("Gap_" + level + "_unnormalized", contiguous, normalize: false);

            TestContext.Out.WriteLine("CONSOLE:\n" + contiguousOutput + "\nNORM:\n" + string.Join("\n", contiguousPeptides.Split('\n').Take(5)) + "\nRAW:\n" + string.Join("\n", unnormalizedPeptides.Split('\n').Take(5)));
            Assert.That(gappedOutput, Does.Contain(expectedWarning));
            Assert.That(gappedOutput, Does.Not.Contain("Error"));
            Assert.That(contiguousOutput, Does.Not.Contain("quantified as numbered"));
            Assert.That(gappedPeptides, Is.Not.Empty);
            Assert.That(contiguousPeptides, Is.Not.EqualTo(unnormalizedPeptides));
            Assert.That(gappedPeptides, Is.EqualTo(contiguousPeptides));
        }

        /// <summary>
        /// Runs the command line on copies of SmallCalibratible_Yeast.mzML, one per design row, each with its
        /// intensities multiplied by the row's scale and its PSMs copied from the MetaMorpheus sample PSMs.
        /// Returns what it printed and the peptide table.
        /// </summary>
        private static (string Console, string Peptides) RunDesign(string folderName,
            (string Name, string Condition, int Biorep, int Fraction, int Techrep, double Scale)[] rows, bool normalize)
        {
            string sampleFiles = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            string folder = Path.Combine(TestContext.CurrentContext.TestDirectory, folderName);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            Directory.CreateDirectory(folder);

            string[] psmLines = File.ReadAllLines(Path.Combine(sampleFiles, "MetaMorpheus", "AllPSMs.psmtsv"));
            var psms = new List<string> { psmLines[0] };
            var design = new List<string> { "FileName	Condition	Biorep	Fraction	Techrep" };

            foreach (var row in rows)
            {
                MsDataFile data = MsDataFileReader.GetDataFile(Path.Combine(sampleFiles, "SmallCalibratible_Yeast.mzML")).LoadAllStaticData();
                foreach (MsDataScan scan in data.GetAllScansList())
                {
                    double[] intensities = scan.MassSpectrum.YArray;
                    for (int i = 0; i < intensities.Length; i++)
                    {
                        intensities[i] *= row.Scale;
                    }
                }
                MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(data, Path.Combine(folder, row.Name + ".mzML"), false);

                psms.AddRange(psmLines.Skip(1).Select(l => row.Name + l.Substring(l.IndexOf('	'))));
                design.Add(string.Join("	", row.Name, row.Condition, row.Biorep, row.Fraction, row.Techrep));
            }

            string psmPath = Path.Combine(folder, "AllPSMs.psmtsv");
            File.WriteAllLines(psmPath, psms);
            File.WriteAllLines(Path.Combine(folder, "ExperimentalDesign.tsv"), design);

            string output = Path.Combine(folder, "output");
            var args = new List<string> { "--rep", folder, "--idt", psmPath, "--out", output, "--ppm", "5" };
            if (normalize)
            {
                // a bare switch: "--nor false" would turn normalization on too
                args.Add("--nor");
            }

            var console = new StringWriter();
            TextWriter original = Console.Out;
            Console.SetOut(console);
            try
            {
                CMD.FlashLfqExecutable.Main(args.ToArray());
            }
            finally
            {
                Console.SetOut(original);
            }

            string peptides = Path.Combine(output, "QuantifiedPeptides.tsv");
            string table = File.Exists(peptides) ? File.ReadAllText(peptides) : "";
            Directory.Delete(folder, true);
            return (console.ToString(), table);
        }
    }
}
