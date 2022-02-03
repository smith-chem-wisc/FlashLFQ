using CMD;
using FlashLFQ;
using IO.MzML;
using MzLibUtil;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Util;

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
        public static void TestPercolatorOutput()
        {
            string search = "Percolator";
            string psmFilename = "percolatorTestData.txt";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", search);
            var pathOfIdentificationFile = Path.Combine(myDirectory, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "percolatorMzml.mzML");
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

            string peaksPath = Path.Combine(myDirectory, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }


        //[Test]
        //public void TestPercolatorReadPsmsGetsRTsFromFileHeader()
        //{
        //    string search = "Percolator";
        //    string psmFilename = "percolatorTestData.txt";

        //    var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", search);
        //    var pathOfIdentificationFile = Path.Combine(myDirectory, psmFilename);
        //    var pathOfMzml = Path.Combine(myDirectory, "percolatorMzml.mzML");
        //    Assert.That(File.Exists(pathOfIdentificationFile));
        //    Assert.That(File.Exists(pathOfMzml));



        //    SpectraFileInfo sfi = new SpectraFileInfo(pathOfMzml, "A", 1, 1, 1);

        //    List<double> expectedRetentionTimes = new List<double> { 7.54, 7.54, 7.56, 7.58, 7.61, 7.63 };

        //    List<Identification> ids = PsmReader.ReadPsms(pathOfIdentificationFile, true, new List<SpectraFileInfo> { sfi });
        //    Assert.AreEqual(6, ids.Count);
        //    List<double> actualRetentionTimes = ids.Select(t => Math.Round(t.Ms2RetentionTimeInMinutes, 2)).ToList();

        //    foreach (double rt in actualRetentionTimes)
        //    {
        //        Assert.IsTrue(Double.IsFinite(rt));
        //    }
        //    CollectionAssert.AreEquivalent(expectedRetentionTimes, actualRetentionTimes);

        //    List<int> proteinGroupCounts = new List<int> { 11, 6, 3, 2, 15, 3 };
        //    CollectionAssert.AreEquivalent(proteinGroupCounts, ids.Select(i => i.ProteinGroups.Count).ToList());
        //}

        //[Test]
        //public static void TestPercolatorErrorHandling()
        //{
        //    string search = "Percolator";
        //    string psmFilename = "BadPercolatorSmallCalibratableYeast.txt";

        //    string myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
        //    string pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
        //    string pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
        //    SpectraFileInfo sfi = new SpectraFileInfo(pathOfMzml, "A", 1, 1, 1);

        //    List<Identification> ids = PsmReader.ReadPsms(pathOfIdentificationFile, false, new List<SpectraFileInfo> { sfi });

        //    //would like better assertion with message but can't get it to return exception message...
        //    Assert.IsEmpty(ids);
        //}

        //[Test]
        //public static void TestGenericOutput()
        //{
        //    string search = "Generic";
        //    string psmFilename = "AllPSMs.tsv";

        //    var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
        //    var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
        //    var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
        //    Assert.That(File.Exists(pathOfIdentificationFile));
        //    Assert.That(File.Exists(pathOfMzml));

        //    string[] myargs = new string[]
        //    {
        //        "--rep",
        //        myDirectory,
        //        "--idt",
        //        pathOfIdentificationFile,
        //        "--ppm",
        //        "5"
        //    };

        //    CMD.FlashLfqExecutable.Main(myargs);

        //    string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
        //    Assert.That(File.Exists(peaksPath));
        //    File.Delete(peaksPath);

        //    string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
        //    Assert.That(File.Exists(peptidesPath));
        //    File.Delete(peptidesPath);

        //    string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
        //    Assert.That(File.Exists(proteinsPath));
        //    File.Delete(proteinsPath);
        //}

        //[Test]
        //public static void TestFlashLfqExecutableWithNormalization()
        //{
        //    string search = "MetaMorpheus";
        //    string psmFilename = "AllPSMs.psmtsv";

        //    var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
        //    var pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
        //    var pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
        //    Assert.That(File.Exists(pathOfIdentificationFile));
        //    Assert.That(File.Exists(pathOfMzml));

        //    string[] myargs = new string[]
        //    {
        //        "--rep",
        //        myDirectory,
        //        "--idt",
        //        pathOfIdentificationFile,
        //        "--ppm",
        //        "5",
        //        "--nor",
        //        "true"
        //    };

        //    CMD.FlashLfqExecutable.Main(myargs);

        //    string peaksPath = Path.Combine(myDirectory, search, "QuantifiedPeaks.tsv");
        //    Assert.That(File.Exists(peaksPath));
        //    File.Delete(peaksPath);

        //    string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
        //    Assert.That(File.Exists(peptidesPath));
        //    File.Delete(peptidesPath);

        //    string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
        //    Assert.That(File.Exists(proteinsPath));
        //    File.Delete(proteinsPath);
        //}

        //[Test]
        /////
        ///// The purpose of this unit test is to ensure that the settings passed by the user through the command-line or the GUI
        ///// are passed propertly into the FlashLFQ engine.
        /////
        //public static void TestSettingsPassing()
        //{
        //    // make settings
        //    FlashLfqSettings settings = new FlashLfqSettings();

        //    // set the settings to non-default values
        //    var properties = settings.GetType().GetProperties();

        //    foreach (var property in properties)
        //    {
        //        Type type = property.PropertyType;

        //        if (type == typeof(string))
        //        {
        //            property.SetValue(settings, "TEST_VALUE");
        //        }
        //        else if (type == typeof(bool) || type == typeof(bool?))
        //        {
        //            if (property.GetValue(settings) == null || (bool)property.GetValue(settings) == false)
        //            {
        //                property.SetValue(settings, true);
        //            }
        //            else
        //            {
        //                property.SetValue(settings, false);
        //            }
        //        }
        //        else if (type == typeof(double) || type == typeof(double?))
        //        {
        //            property.SetValue(settings, double.MinValue);
        //        }
        //        else if (type == typeof(int) || type == typeof(int?))
        //        {
        //            property.SetValue(settings, int.MinValue);
        //        }
        //        else
        //        {
        //            Assert.IsTrue(false);
        //        }
        //    }

        //    settings.MaxThreads = 1;

        //    FlashLfqEngine e = FlashLfqSettings.CreateEngineWithSettings(settings, new List<Identification>());

        //    var engineProperties = e.GetType().GetFields();

        //    // check to make sure the settings got passed properly into the engine (the settings should have identical values)
        //    foreach (var property in properties)
        //    {
        //        string name = property.Name;

        //        // skip settings that don't exist in the FlashLFQ engine
        //        // these are usually just command-line options for i/o stuff, etc.
        //        if (name == "PsmIdentificationPath"
        //            || name == "SpectraFileRepository"
        //            || name == "OutputPath"
        //            || name == "ReadOnlyFileSystem"
        //            || name == "PrintThermoLicenceViaCommandLine"
        //            || name == "AcceptThermoLicenceViaCommandLine")
        //        {
        //            continue;
        //        }

        //        var engineProperty = engineProperties.First(p => p.Name == name);

        //        var settingsValue = property.GetValue(settings);
        //        var engineValue = engineProperty.GetValue(e);

        //        Assert.AreEqual(settingsValue, engineValue);
        //    }
        //}
    }
}
