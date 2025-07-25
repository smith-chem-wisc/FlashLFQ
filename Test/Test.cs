﻿using CMD;
using FlashLFQ;
using IO.MzML;
using MzLibUtil;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Util;
using Easy.Common.Extensions;

namespace Test
{
    [TestFixture]
    internal class Test
    {
        [Test]
        [TestCase("MetaMorpheus", "AllPSMs.psmtsv")]
        [TestCase("MetaMorpheus", "AllPeptides_NewHeader.psmtsv")]
        [TestCase("Morpheus", "SmallCalibratible_Yeast.PSMs.tsv")]
        [TestCase("MaxQuant", "msms.txt")]
        [TestCase("PeptideShaker", "Default PSM Report.tabular")]
        [TestCase("Percolator", "percolatorTestData.txt", "Percolator\\percolatorMzml.mzML")]
        [TestCase("Generic", "AllPSMs.tsv")]
        [TestCase("MsFragger", "Fragger_v4p3_psm.tsv")]
        public static void TestOutputs(string search, string psmFilename, string mzmlFileName = "SmallCalibratible_Yeast.mzML")
        {
            var defaultDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(defaultDirectory, search, psmFilename);
            var pathOfMzml = Path.Combine(defaultDirectory, mzmlFileName);
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            var myDirectory = Path.GetDirectoryName(pathOfMzml);

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

            string peaksPath = Path.Combine(defaultDirectory, search, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(defaultDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(defaultDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }

        [Test]
        public void TestPercolatorReadPsmsGetsRTsFromFileHeader()
        {
            string search = "Percolator";
            string psmFilename = "percolatorTestData.txt";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", search);
            var pathOfIdentificationFile = Path.Combine(myDirectory, psmFilename);
            var pathOfMzml = Path.Combine(myDirectory, "percolatorMzml.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            SpectraFileInfo sfi = new SpectraFileInfo(pathOfMzml, "A", 1, 1, 1);

            List<double> expectedRetentionTimes = new() { 7.54, 7.54, 7.56, 7.58, 7.61, 7.63 };

            PsmReader psmReader = new();
            List<Identification> ids = psmReader.ReadPsms(pathOfIdentificationFile, true, new List<SpectraFileInfo> { sfi });
            Assert.AreEqual(6, ids.Count);

            //strings separated by commas should be taken literally as protein names
            Assert.AreEqual(11, ids[0].ProteinGroups.Count);
            List<string> expectedProteinNameStrings = new() { "sp|Q13885|TBB2A_HUMAN", "tr|M0R1I1|M0R1I1_HUMAN", "tr|Q5JP53|Q5JP53_HUMAN",
                "tr|M0QZL7|M0QZL7_HUMAN", "sp|P04350|TBB4A_HUMAN", "sp|P07437|TBB5_HUMAN", "sp|Q9BVA1|TBB2B_HUMAN", "tr|M0R278|M0R278_HUMAN",
                "tr|Q5ST81|Q5ST81_HUMAN", "sp|P68371|TBB4B_HUMAN", "tr|M0QY85|M0QY85_HUMAN" };
            CollectionAssert.AreEquivalent(ids[0].ProteinGroups.Select(n => n.ProteinGroupName).ToList(), expectedProteinNameStrings);

            List<double> actualRetentionTimes = ids.Select(t => Math.Round(t.Ms2RetentionTimeInMinutes, 2)).ToList();

            foreach (double rt in actualRetentionTimes)
            {
                Assert.IsTrue(Double.IsFinite(rt));
            }
            CollectionAssert.AreEquivalent(expectedRetentionTimes, actualRetentionTimes);

            List<int> proteinGroupCounts = new() { 11, 6, 3, 2, 15, 3 };
            CollectionAssert.AreEquivalent(proteinGroupCounts, ids.Select(i => i.ProteinGroups.Count).ToList());
        }

        [Test]
        public static void TestPercolatorErrorHandling()
        {
            string search = "Percolator";
            string psmFilename = "BadPercolatorSmallCalibratableYeast.txt";

            string myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            string pathOfIdentificationFile = Path.Combine(myDirectory, search, psmFilename);
            string pathOfMzml = Path.Combine(myDirectory, "SmallCalibratible_Yeast.mzML");
            SpectraFileInfo sfi = new SpectraFileInfo(pathOfMzml, "A", 1, 1, 1);

            PsmReader psmReader = new();
            List<Identification> ids = psmReader.ReadPsms(pathOfIdentificationFile, false, new List<SpectraFileInfo> { sfi });

            //would like better assertion with message but can't get it to return exception message...
            Assert.IsEmpty(ids);
        }

        [Test]
        public static void TestGenericOutputAdditionalColumns()
        {
            string search = "Generic";
            string psmFilename = "NewGenericInput.tsv";

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

            bool checkThatPeakIsWritten = false;
            //Check that decoy peptide peak is reported and marked as decoy
            using (var reader = new StreamReader(peaksPath))
            {
                var header = reader.ReadLine().Split('\t');
                int sequenceIdx = header.IndexOf("Full Sequence");
                int tdIndex = header.IndexOf("Decoy Peptide");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var split = line.Split('\t');
                    if (split[sequenceIdx] == "AVTVHSK")
                    {
                        Assert.That(split[tdIndex] == "True");
                        checkThatPeakIsWritten = true;
                        break;
                    }
                }
            }
            Assert.That(checkThatPeakIsWritten);
            File.Delete(peaksPath);

            string peptidesPath = Path.Combine(myDirectory, search, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));

            //Check that decoy peptide and peptide with QValue > 0.01 are not reported
            //Check that peptide with Q value == 0.01 is reported
            bool checkThatPeptideIsWritten = false;
            using (var reader = new StreamReader(peptidesPath))
            {
                var header = reader.ReadLine().Split('\t');
                int sequenceIdx = header.IndexOf("Sequence");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var split = line.Split('\t');
                    if (split[sequenceIdx] == "KMTSSSK")
                    {
                        checkThatPeptideIsWritten = true;
                    }
                    if (split[sequenceIdx] == "FVM[Common Variable:Oxidation on M]EIAK" || split[sequenceIdx] == "AVTVHSK")
                    {
                        Assert.Fail(split[sequenceIdx] + " was incorrectly written to QuantifiedPeptides");
                    }
                }
            }
            Assert.That(checkThatPeptideIsWritten);
            File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(myDirectory, search, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            File.Delete(proteinsPath);
        }

        [Test]
        public static void TestParallelProcessingMetaMorpheusOutput()
        {
            string search = "Parallel";
            string psmFilename = "AllPSMs.psmtsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", search);
            var pathOfIdentificationFile = Path.Combine(myDirectory, psmFilename);
            var pathOfMzml1 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_3.mzML");
            var pathOfMzml2 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_4.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml1));
            Assert.That(File.Exists(pathOfMzml2));

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

        [Test]
        public static void TestParallelProcessingMetaMorpheusOutputWithExtensions()
        {
            string search = "Parallel";
            string psmFilename = "withExtensions_AllPSMs.psmtsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", search);
            var pathOfIdentificationFile = Path.Combine(myDirectory, psmFilename);
            var pathOfMzml1 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_3.mzML");
            var pathOfMzml2 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_4.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml1));
            Assert.That(File.Exists(pathOfMzml2));

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

        [Test]
        public static void TestParallelProcessingMetaMorpheusOutputWithExtensionsAndWindowsPath()
        {
            string search = "Parallel";
            string psmFilename = "withExtensionsAndWindowsPath_AllPSMs.psmtsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", search);
            var pathOfIdentificationFile = Path.Combine(myDirectory, psmFilename);
            var pathOfMzml1 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_3.mzML");
            var pathOfMzml2 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_4.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml1));
            Assert.That(File.Exists(pathOfMzml2));

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

        [Test]
        public static void TestParallelProcessingMetaMorpheusOutputWithExtensionsAndLinuxPath()
        {
            string search = "Parallel";
            string psmFilename = "withExtensionsAndLinuxPath_AllPSMs.psmtsv";

            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", search);
            var pathOfIdentificationFile = Path.Combine(myDirectory, psmFilename);
            var pathOfMzml1 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_3.mzML");
            var pathOfMzml2 = Path.Combine(myDirectory, "20100614_Velos1_TaGe_SA_Jurkat_4.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml1));
            Assert.That(File.Exists(pathOfMzml2));

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


            var engineProperties = e.FlashParams.GetType().GetProperties();

            // check to make sure the settings got passed properly into the engine (the settings should have identical values)
            foreach (var property in properties)
            {
                string name = property.Name;

                // skip settings that don't exist in the FlashLFQ engine
                // these are usually just command-line options for i/o stuff, etc.
                if (name == "PsmIdentificationPath"
                    || name == "SpectraFileRepository"
                    || name == "OutputPath"
                    || name == "ReadOnlyFileSystem"
                    || name == "PrintThermoLicenceViaCommandLine"
                    || name == "AcceptThermoLicenceViaCommandLine"
                    || name == "PeptideIdentificationPath"
                    || name == "UsePepQValue")
                {
                    continue;
                }

                var engineProperty = engineProperties.FirstOrDefault(p => p.Name == name);

                var settingsValue = property.GetValue(settings);
                var engineValue = engineProperty.GetValue(e.FlashParams);

                Assert.That(settingsValue, Is.EqualTo(engineValue));
            }
        }

        [Test]
        public static void TestReadPsmsThrowsException()
        {
            // Arrange  
            string invalidFilePath = "NonExistentFile.psmtsv";
            PsmReader psmReader = new();

            // Act & Assert  
            Assert.DoesNotThrow(() => psmReader.ReadPsms(invalidFilePath, false, new List<SpectraFileInfo>()));
        }
    }
}
