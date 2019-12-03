using CMD;
using FlashLFQ;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        [Test]
        ///
        /// The purpose of this unit test is to ensure that the settings passed by the user through the command-line or the GUI
        /// are passed propertly into the FlashLFQ engine.
        ///
        public static void TestSettingsPassing()
        {
            // make settings
            FlashLfqSettings settings = new FlashLfqSettings();

            // set the settings to non-default values
            var properties = settings.GetType().GetProperties();

            foreach (var property in properties)
            {
                Type type = property.PropertyType;

                if (type == typeof(string))
                {
                    property.SetValue(settings, "TEST_VALUE");
                }
                else if (type == typeof(bool) || type == typeof(bool?))
                {
                    if (property.GetValue(settings) == null || (bool)property.GetValue(settings) == false)
                    {
                        property.SetValue(settings, true);
                    }
                    else
                    {
                        property.SetValue(settings, false);
                    }
                }
                else if (type == typeof(double) || type == typeof(double?))
                {
                    property.SetValue(settings, double.MinValue);
                }
                else if (type == typeof(int) || type == typeof(int?))
                {
                    property.SetValue(settings, int.MinValue);
                }
                else
                {
                    Assert.IsTrue(false);
                }
            }

            settings.MaxThreads = 1;

            FlashLfqEngine e = FlashLfqSettings.CreateEngineWithSettings(settings, new List<Identification>());

            var engineProperties = e.GetType().GetFields();

            // check to make sure the settings got passed properly into the engine (the settings should have identical values)
            foreach (var property in properties)
            {
                string name = property.Name;

                if (name == "PsmIdentificationPath"
                    || name == "SpectraFileRepository"
                    || name == "OutputPath")
                {
                    continue;
                }

                var engineProperty = engineProperties.First(p => p.Name == name);

                var settingsValue = property.GetValue(settings);
                var engineValue = engineProperty.GetValue(e);

                Assert.AreEqual(settingsValue, engineValue);
            }
        }
    }
}
