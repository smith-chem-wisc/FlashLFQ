using FlashLFQ;
using MassSpectrometry;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Util;

namespace Test
{
    /// <summary>
    /// Tests for FlashLFQ's RNA/oligonucleotide support, which is driven by the "RNA Mode" option and
    /// by supplying an .osmtsv (oligo spectrum match) identification file. RNA is quantified in negative
    /// mode, so identifications carry negative charge states, and the engine builds theoretical isotope
    /// distributions from the ribonucleotide model rather than the amino-acid one.
    /// </summary>
    [TestFixture]
    internal class TestRna
    {
        private static string RnaDirectory =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", "RNA");

        /// <summary>
        /// The .osmtsv file is read through the same IQuantifiableResultFile path the app uses. Every
        /// identification it produces should be an RNA oligo: a ribonucleotide base sequence carrying a
        /// negative precursor charge.
        /// </summary>
        [Test]
        public static void TestReadRnaOsmtsvIdentifications()
        {
            string osmPath = Path.Combine(RnaDirectory, "OsmFileForTesting.osmtsv");
            Assert.That(File.Exists(osmPath));

            // The OSM file references two spectra files. Their contents are irrelevant to reading the
            // identifications - only the file names have to match - so placeholder SpectraFileInfos suffice.
            var spectraFiles = new List<SpectraFileInfo>
            {
                new SpectraFileInfo(Path.Combine(RnaDirectory, "20250612_RNA-Mix_10V.mzML"), "RNA", 0, 0, 0),
                new SpectraFileInfo(Path.Combine(RnaDirectory, "20250612_RNA-Mix_30V.mzML"), "RNA", 1, 0, 0),
            };

            List<Identification> ids = new PsmReader().ReadPsms(osmPath, silent: true, spectraFiles);

            Assert.AreEqual(6, ids.Count);
            // RNA is ionized in negative mode; these identifications are all 5- precursors.
            Assert.IsTrue(ids.All(id => id.PrecursorChargeState < 0));
            CollectionAssert.AreEquivalent(new[] { -5 }, ids.Select(id => id.PrecursorChargeState).Distinct().ToArray());
            // Base sequences are ribonucleotides (A/C/G/U only), not amino acids.
            Assert.IsTrue(ids.All(id => id.BaseSequence.All(c => "ACGU".Contains(c))));
            Assert.IsTrue(ids.All(id => id.BaseSequence == "UUCAAGUAAUCCAGGAUAGGCU"));
            // Monoisotopic masses for a 22mer oligo land in the ~7000 Da range.
            Assert.IsTrue(ids.All(id => id.MonoisotopicMass > 6900 && id.MonoisotopicMass < 7100));
        }

        /// <summary>
        /// Verifies the RNA Mode setting flows from FlashLfqSettings into the engine parameters. This is
        /// the switch the GUI checkbox and the CMD --rna/.osmtsv detection both toggle.
        /// </summary>
        [Test]
        public static void TestRnaModeSettingReachesEngine()
        {
            var id = MakeRnaIdentification();

            var rnaSettings = new FlashLfqSettings { RnaMode = true, MaxThreads = 1 };
            FlashLfqEngine rnaEngine = FlashLfqSettings.CreateEngineWithSettings(rnaSettings, new List<Identification> { id });
            Assert.IsTrue(rnaEngine.FlashParams.RnaMode);

            var peptideSettings = new FlashLfqSettings { RnaMode = false, MaxThreads = 1 };
            FlashLfqEngine peptideEngine = FlashLfqSettings.CreateEngineWithSettings(peptideSettings, new List<Identification> { id });
            Assert.IsFalse(peptideEngine.FlashParams.RnaMode);
        }

        /// <summary>
        /// RNA Mode defaults to off, matching the historical peptide-only behavior.
        /// </summary>
        [Test]
        public static void TestRnaModeDefaultsOff()
        {
            Assert.IsFalse(new FlashLfqSettings().RnaMode);
            Assert.IsFalse(new FlashLfqParameters().RnaMode);
        }

        /// <summary>
        /// Full RNA quantification, exercised only when RNA spectra matching the OSM's file names are
        /// present in SampleFiles/RNA. Any .raw or .mzML dropped there whose name matches a "File Name"
        /// in the OSM file makes this run a real end-to-end RNA quantification; otherwise it is ignored.
        /// </summary>
        [Test]
        public static void TestRnaEndToEndQuantification()
        {
            string osmPath = Path.Combine(RnaDirectory, "OsmFileForTesting.osmtsv");
            Assert.That(File.Exists(osmPath));

            var osmFileNames = File.ReadAllLines(osmPath).Skip(1)
                .Where(line => line.Length > 0)
                .Select(line => line.Split('\t')[0])
                .Distinct()
                .ToHashSet();

            var spectraPaths = Directory.GetFiles(RnaDirectory)
                .Where(f => new[] { ".raw", ".mzml" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Where(f => osmFileNames.Contains(Path.GetFileNameWithoutExtension(f)))
                .OrderBy(f => f)
                .ToList();

            if (!spectraPaths.Any())
            {
                Assert.Ignore("No RNA spectra (.raw/.mzML) matching the OSM file names were found in " +
                    "SampleFiles/RNA. Drop matching spectra there to run a full RNA quantification.");
            }

            var spectraFiles = spectraPaths
                .Select((path, i) => new SpectraFileInfo(path, "RNA", i, 0, 0))
                .ToList();

            List<Identification> ids = new PsmReader().ReadPsms(osmPath, silent: true, spectraFiles);
            Assert.IsNotEmpty(ids);

            var settings = new FlashLfqSettings { RnaMode = true, MaxThreads = 1 };
            FlashLfqEngine engine = FlashLfqSettings.CreateEngineWithSettings(settings, ids);
            FlashLfqResults results = engine.Run();

            Assert.IsNotNull(results);
            Assert.IsTrue(results.Peaks.Values.Any(peakList => peakList.Any()),
                "RNA quantification produced no chromatographic peaks.");
        }

        /// <summary>
        /// A real RNA oligonucleotide identification (a 12mer) with a negative charge state, used to build
        /// an engine without needing spectra on disk.
        /// </summary>
        private static Identification MakeRnaIdentification()
        {
            var rna = new global::Transcriptomics.RNA("GUACGUACGUAC");
            var file = new SpectraFileInfo(Path.Combine(RnaDirectory, "rna.mzML"), "RNA", 0, 0, 0);
            return new Identification(file, "GUACGUACGUAC", "GUACGUACGUAC",
                rna.MonoisotopicMass, 5.0, -3, new List<ProteinGroup>());
        }
    }
}
