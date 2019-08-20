using NUnit.Framework;
using System.IO;

namespace Test
{
    [TestFixture]
    internal class Test
    {
        [Test]
        public static void TestMetaMorpheusOutput()
        {
            string search = "MetaMorpheus";
            string psmFilename = "AllPSMs.psmtsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            string[] myargs = new string[]
            {
                "--rep",
                myDirectory,
                "--idt",
                pathOfIdentificationFile,
                "--ppm",
                "5"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }

        [Test]
        public static void TestMorpheusOutput()
        {
            string search = "Morpheus";
            string psmFilename = "SmallCalibratible_Yeast.PSMs.tsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            string[] myargs = new string[]
            {
                "--rep",
                myDirectory,
                "--idt",
                pathOfIdentificationFile,
                "--ppm",
                "5"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }

        [Test]
        public static void TestMaxQuantOutput()
        {
            string search = "MaxQuant";
            string psmFilename = "msms.txt";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            string[] myargs = new string[]
            {
                "--rep",
                myDirectory,
                "--idt",
                pathOfIdentificationFile,
                "--ppm",
                "5"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }

        [Test]
        public static void TestPeptideShakerOutput()
        {
            string search = "PeptideShaker";
            string psmFilename = "Default PSM Report.tabular";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            string[] myargs = new string[]
            {
                "--rep",
                myDirectory,
                "--idt",
                pathOfIdentificationFile,
                "--ppm",
                "5"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }

        [Test]
        public static void TestGenericOutput()
        {
            string search = "Generic";
            string psmFilename = "AllPSMs.tsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            string[] myargs = new string[]
            {
                "--rep",
                myDirectory,
                "--idt",
                pathOfIdentificationFile,
                "--ppm",
                "5"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }
        
        [Test]
        public static void TestFlashLfqExecutableWithNormalization()
        {
            string search = "MetaMorpheus";
            string psmFilename = "AllPSMs.psmtsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            string[] myargs = new string[]
            {
                "--rep",
                myDirectory,
                "--idt",
                pathOfIdentificationFile,
                "--ppm",
                "5",
                "--nor",
                "true"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }
    }
}
