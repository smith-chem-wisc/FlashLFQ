using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FlashLFQ;
using IO.Thermo;
using System.IO;
using UsefulProteomicsDatabases;

namespace Test
{
    [TestFixture]
    class Test
    {
        //[Test]
        //public static void MyTest()
        //{
        //    Console.WriteLine("UNIT TEST - Entering unit test");


        //    string idtArg = @"E:\Projects\IonStar_eColi\AC.txt"; //identification file
        //    string bftArg = @"E:\Projects\IonStar_eColi\filename_CBFT.txt";
        //    string repArg = @"E:\Projects\IonStar_eColi";
        //    string silArg = "true";
        //    string normArg = "true";

        //    string[] args = new string[] { "--idt " + idtArg, "--bft " + bftArg, "--rep " + repArg, "--sil" + silArg, "--nrm" + normArg};

        //    FlashLFQEngine engine = new FlashLFQEngine();
        //    engine.globalStopwatch.Start();


        //    if (!engine.ReadPeriodicTable(null))
        //        return;

        //    if (!engine.ParseArgs(args))
        //        return;

        //    if (!engine.ReadCBFTKey())
        //        return;

        //    if (!engine.ReadIdentificationsFromTSV())
        //        return;

        //    engine.ConstructIndexTemplateFromIdentifications();


        //    for (int i = 0; i < engine.rawFileInfos.Count; i++)
        //    {
        //        if (!engine.Quantify(null, engine.rawFileInfos[i].fullFilePath))
        //            return;
        //    }



        //    if (engine.mbr)
        //        engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();


        //     if (!engine.WriteResults("_FlashLFQ_", true, true, true))
        //        return;

        //    if (!engine.Normalize())
        //        return;

        //    if (!engine.WriteNormalizedResults("_FlashLFQ_Normalized", true))
        //        return;
        //    if (!engine.WriteNormalizedResults("_FlashLFQ_Normalized", false))
        //        return;


        //    Assert.AreEqual(1, 1);
        //}


        [Test]
        public static void TestNormalizationOneFeature()
        {

            
            List<RawFileInfo> fpCBFT = new List<RawFileInfo>();
            RawFileInfo r1 = new RawFileInfo("filename-1", "C1", "B", "F1", "T");
            RawFileInfo r2 = new RawFileInfo("filename-2", "C2", "B", "F1", "T");

            RawFileInfo r3 = new RawFileInfo("filename-3", "C1", "B", "F2", "T");
            RawFileInfo r4 = new RawFileInfo("filename-4", "C2", "B", "F2", "T");


            fpCBFT.Add(r1);
            fpCBFT.Add(r2);
            fpCBFT.Add(r3);
            fpCBFT.Add(r4);

            List<Peptide> peptideFeatures = new List<Peptide>();
            Peptide.rawFiles = fpCBFT;
            Peptide p1 = new Peptide("PEPTIDE", "ProteinGroup1");            
            p1.StashIntensities(r1, 1);
            p1.StashIntensities(r2, 2);
            p1.StashIntensities(r3, 3);
            p1.StashIntensities(r4, 6);
            peptideFeatures.Add(p1);



            Normalization N = new Normalization();

            peptideFeatures = N.Normalize(peptideFeatures, fpCBFT);




            Assert.AreEqual(5, Math.Round(peptideFeatures[0].quantities.conditions[0].bioreps[0].fractions[0].normalizationFactor),0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[0].bioreps[0].fractions[1].normalizationFactor),0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[1].bioreps[0].fractions[0].normalizationFactor),0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[1].bioreps[0].fractions[1].normalizationFactor),0);
        }


        [Test]
        public static void TestNormalizationTwoFeatureMatched()
        {


            List<RawFileInfo> fpCBFT = new List<RawFileInfo>();
            RawFileInfo r1 = new RawFileInfo("filename-1", "C1", "B", "F1", "T");
            RawFileInfo r2 = new RawFileInfo("filename-2", "C2", "B", "F1", "T");

            RawFileInfo r3 = new RawFileInfo("filename-3", "C1", "B", "F2", "T");
            RawFileInfo r4 = new RawFileInfo("filename-4", "C2", "B", "F2", "T");


            fpCBFT.Add(r1);
            fpCBFT.Add(r2);
            fpCBFT.Add(r3);
            fpCBFT.Add(r4);

            List<Peptide> peptideFeatures = new List<Peptide>();
            Peptide.rawFiles = fpCBFT;
            Peptide p1 = new Peptide("PEPTIDE1", "ProteinGroup1");
            p1.StashIntensities(r1, 1);
            p1.StashIntensities(r2, 2);
            p1.StashIntensities(r3, 3);
            p1.StashIntensities(r4, 6);

            peptideFeatures.Add(p1);

            Peptide p2 = new Peptide("PEPTIDE2", "ProteinGroup1");
            p2.StashIntensities(r1, 0.5);
            p2.StashIntensities(r2, 1);
            p2.StashIntensities(r3, 1.5);
            p2.StashIntensities(r4, 3);

            peptideFeatures.Add(p2);

            Normalization N = new Normalization();

            peptideFeatures = N.Normalize(peptideFeatures, fpCBFT);


            Assert.AreEqual(5, Math.Round(peptideFeatures[0].quantities.conditions[0].bioreps[0].fractions[0].normalizationFactor),0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[0].bioreps[0].fractions[1].normalizationFactor),0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[1].bioreps[0].fractions[0].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[1].bioreps[0].fractions[1].normalizationFactor), 0);
            Assert.AreEqual(5, Math.Round(peptideFeatures[1].quantities.conditions[0].bioreps[0].fractions[0].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[1].quantities.conditions[0].bioreps[0].fractions[1].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[1].quantities.conditions[1].bioreps[0].fractions[0].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[1].quantities.conditions[1].bioreps[0].fractions[1].normalizationFactor), 0);
        }


        [Test]
        public static void TestNormalizationTwoFeatureMisatched()
        {


            List<RawFileInfo> fpCBFT = new List<RawFileInfo>();
            RawFileInfo r1 = new RawFileInfo("filename-1", "C1", "B", "F1", "T");
            RawFileInfo r2 = new RawFileInfo("filename-2", "C2", "B", "F1", "T");

            RawFileInfo r3 = new RawFileInfo("filename-3", "C1", "B", "F2", "T");
            RawFileInfo r4 = new RawFileInfo("filename-4", "C2", "B", "F2", "T");


            fpCBFT.Add(r1);
            fpCBFT.Add(r2);
            fpCBFT.Add(r3);
            fpCBFT.Add(r4);

            List<Peptide> peptideFeatures = new List<Peptide>();
            Peptide.rawFiles = fpCBFT;
            Peptide p1 = new Peptide("PEPTIDE1", "ProteinGroup1");
            p1.StashIntensities(r1, 1);
            p1.StashIntensities(r2, 2);
            p1.StashIntensities(r3, 3);
            p1.StashIntensities(r4, 6);

            peptideFeatures.Add(p1);

            Peptide p2 = new Peptide("PEPTIDE2", "ProteinGroup1");
            p2.StashIntensities(r1, 0.5);
            p2.StashIntensities(r3, 1);


            peptideFeatures.Add(p2);

            Normalization N = new Normalization();

            peptideFeatures = N.Normalize(peptideFeatures, fpCBFT);


            Assert.AreEqual(5, Math.Round(peptideFeatures[0].quantities.conditions[0].bioreps[0].fractions[0].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[0].bioreps[0].fractions[1].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[1].bioreps[0].fractions[0].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[0].quantities.conditions[1].bioreps[0].fractions[1].normalizationFactor), 0);
            Assert.AreEqual(5, Math.Round(peptideFeatures[1].quantities.conditions[0].bioreps[0].fractions[0].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[1].quantities.conditions[0].bioreps[0].fractions[1].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[1].quantities.conditions[1].bioreps[0].fractions[0].normalizationFactor), 0);
            Assert.AreEqual(1, Math.Round(peptideFeatures[1].quantities.conditions[1].bioreps[0].fractions[1].normalizationFactor), 0);
        }


        [Test]
        public static void TestEverything()
        {
            Console.WriteLine("UNIT TEST - Entering unit test");
            string elements = Path.Combine(TestContext.CurrentContext.TestDirectory, "elements.dat");
            string files = TestContext.CurrentContext.TestDirectory;
            string ident = Path.Combine(TestContext.CurrentContext.TestDirectory, "aggregatePSMs_5ppmAroundZero.psmtsv");
            
            FlashLFQEngine engine = new FlashLFQEngine();
            Console.WriteLine("UNIT TEST - About to load elements");
            Loaders.LoadElements(elements);
            Console.WriteLine("UNIT TEST - Finished loading elements");

            Assert.That(engine.ParseArgs(new string[] {
                        "--idt " + ident,
                        "--rep " + files,
                        "--ppm 5",
                        "--sil false",
                        "--pau false",
                        "--mbr true" }
                    ));
            Console.WriteLine("UNIT TEST - Done making engine");
            engine.globalStopwatch.Start();
            Assert.That(engine.outputFolder != null);
            engine.SetParallelization(1);

            //Assert.That(engine.ReadPeriodicTable());

            Console.WriteLine("UNIT TEST - About to read TSV file");
            Assert.That(engine.ReadIdentificationsFromTSV());
            Console.WriteLine("UNIT TEST - Finished reading TSV");
            engine.ConstructIndexTemplateFromIdentifications();
            Console.WriteLine("UNIT TEST - Finished constructing bins");
            Assert.That(engine.observedMzsToUseForIndex.Count > 0);
            Assert.That(engine.baseSequenceToIsotopicDistribution.Count > 0);
            Console.WriteLine("UNIT TEST - Bins are OK");

            for (int i = 0; i < engine.rawFileInfos.Count; i++)
            {
                Console.WriteLine("UNIT TEST - Quantifying file " + (i + 1));
                try
                {
                    Assert.That(engine.Quantify(null, engine.rawFileInfos[i].fullFilePath));
                }
                catch (AssertionException)
                {
                    Console.WriteLine("UNIT TEST - Could not quantify file \"" + engine.rawFileInfos[i].fullFilePath + "\"");
                }
            }

            //if (engine.mbr)
            //    engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();

            Console.WriteLine("UNIT TEST - Quantifying proteins ");
            engine.QuantifyProteins();

            Console.WriteLine("UNIT TEST - Asserting results");
            Assert.That(engine.SumFeatures(engine.rawFileInfos.SelectMany(p => p.peaksForThisFile), true).Any());
            Assert.That(engine.SumFeatures(engine.rawFileInfos.SelectMany(p => p.peaksForThisFile), false).Any());

            Assert.That(engine.rawFileInfos[0].peaksForThisFile.First().intensity > 0);
            Assert.That(engine.rawFileInfos[1].peaksForThisFile.First().intensity > 0);

            Assert.That(engine.rawFileInfos[0].peaksForThisFile.Count == 1);
            Assert.That(engine.rawFileInfos[1].peaksForThisFile.Count == 1);

            Assert.That(!engine.rawFileInfos[0].peaksForThisFile.First().isMbrFeature);
            Assert.That(!engine.rawFileInfos[1].peaksForThisFile.First().isMbrFeature);
            Console.WriteLine("UNIT TEST - All passed");
        }

        [Test]
        public static void TestExternalNoPassedFile()
        {
            Console.WriteLine("UNIT TEST - Entering unit test");
            string[] filePaths = Directory.GetFiles(TestContext.CurrentContext.TestDirectory).Where(f => f.Substring(f.IndexOf('.')).ToUpper().Equals(".RAW") || f.Substring(f.IndexOf('.')).ToUpper().Equals(".MZML")).ToArray();
            string elements = Path.Combine(TestContext.CurrentContext.TestDirectory, "elements.dat");
            string ident = Path.Combine(TestContext.CurrentContext.TestDirectory, "aggregatePSMs_5ppmAroundZero.psmtsv");

            FlashLFQEngine engine = new FlashLFQEngine();
            Console.WriteLine("UNIT TEST - About to load elements");
            Loaders.LoadElements(elements);
            Console.WriteLine("UNIT TEST - Finished loading elements");

            engine.PassFilePaths(filePaths);
            Assert.That(engine.ParseArgs(new string[] {
                        "--ppm 5",
                        "--sil false",
                        "--pau false",
                        "--mbr true" }
                    ));
            Console.WriteLine("UNIT TEST - Done making engine");
            engine.globalStopwatch.Start();
            engine.SetParallelization(1);
            
            Console.WriteLine("UNIT TEST - Adding identifications");
            var ids = File.ReadAllLines(ident);
            int lineCount = 1;
            foreach(var line in ids)
            {
                if(lineCount != 1)
                {
                    var splitLine = line.Split('\t');
                    engine.AddIdentification(Path.GetFileNameWithoutExtension(splitLine[0]), splitLine[20], splitLine[21], double.Parse(splitLine[27]), double.Parse(splitLine[2]), (int) double.Parse(splitLine[6]), new List<string> { splitLine[14] });
                }
                lineCount++;
            } 
            Console.WriteLine("UNIT TEST - Finished adding IDs");

            engine.ConstructIndexTemplateFromIdentifications();
            Console.WriteLine("UNIT TEST - Finished constructing bins");
            Assert.That(engine.observedMzsToUseForIndex.Count > 0);
            Assert.That(engine.baseSequenceToIsotopicDistribution.Count > 0);
            Console.WriteLine("UNIT TEST - Bins are OK");

            for (int i = 0; i < engine.rawFileInfos.Count; i++)
            {
                Console.WriteLine("UNIT TEST - Quantifying file " + (i + 1));
                try
                {
                    Assert.That(engine.Quantify(null, engine.rawFileInfos[i].fullFilePath));
                }
                catch (AssertionException)
                {
                    Console.WriteLine("UNIT TEST - Could not quantify file \"" + engine.rawFileInfos[i].fullFilePath + "\"");
                }
            }

            //if (engine.mbr)
            //    engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();

            Console.WriteLine("UNIT TEST - Quantifying proteins ");
            engine.QuantifyProteins();

            Console.WriteLine("UNIT TEST - Asserting results");
            Assert.That(engine.SumFeatures(engine.rawFileInfos.SelectMany(p => p.peaksForThisFile), true).Any());
            Assert.That(engine.SumFeatures(engine.rawFileInfos.SelectMany(p => p.peaksForThisFile), false).Any());

            Assert.That(engine.rawFileInfos[0].peaksForThisFile.First().intensity > 0);
            Assert.That(engine.rawFileInfos[1].peaksForThisFile.First().intensity > 0);

            Assert.That(engine.rawFileInfos[0].peaksForThisFile.Count == 1);
            Assert.That(engine.rawFileInfos[1].peaksForThisFile.Count == 1);

            Assert.That(!engine.rawFileInfos[0].peaksForThisFile.First().isMbrFeature);
            Assert.That(!engine.rawFileInfos[1].peaksForThisFile.First().isMbrFeature);
            Console.WriteLine("UNIT TEST - All passed");
        }

        [Test]
        public static void TestExternalPassedFile()
        {
            Console.WriteLine("UNIT TEST - Entering unit test");
            string[] filePaths = Directory.GetFiles(TestContext.CurrentContext.TestDirectory).Where(f => f.Substring(f.IndexOf('.')).ToUpper().Equals(".RAW")).ToArray();
            var thermoFile = ThermoDynamicData.InitiateDynamicConnection(filePaths[0]);
            
            
            
            
            //This is messed up. we should be able to load static data but cannot in flashlfq. This same command is fine in mzlib
            //var thermoFile = ThermoStaticData.LoadAllStaticData(filePaths[0]);




            string elements = Path.Combine(TestContext.CurrentContext.TestDirectory, "elements.dat");
            string ident = Path.Combine(TestContext.CurrentContext.TestDirectory, "aggregatePSMs_5ppmAroundZero.psmtsv");

            FlashLFQEngine engine = new FlashLFQEngine();
            Console.WriteLine("UNIT TEST - About to load elements");
            Loaders.LoadElements(elements);
            Console.WriteLine("UNIT TEST - Finished loading elements");

            engine.PassFilePaths(filePaths);
            Assert.That(engine.ParseArgs(new string[] {
                        "--ppm 5",
                        "--sil false",
                        "--pau false",
                        "--mbr true" }
                    ));
            Console.WriteLine("UNIT TEST - Done making engine");
            engine.globalStopwatch.Start();
            engine.SetParallelization(1);

            Console.WriteLine("UNIT TEST - Adding identifications");
            var ids = File.ReadAllLines(ident);
            int lineCount = 1;
            foreach (var line in ids)
            {
                if (lineCount != 1)
                {
                    var splitLine = line.Split('\t');
                    engine.AddIdentification(Path.GetFileNameWithoutExtension(splitLine[0]), splitLine[20], splitLine[21], double.Parse(splitLine[27]), double.Parse(splitLine[2]), (int)double.Parse(splitLine[6]), new List<string> { splitLine[14] });
                }
                lineCount++;
            }
            Console.WriteLine("UNIT TEST - Finished adding IDs");

            engine.ConstructIndexTemplateFromIdentifications();
            Console.WriteLine("UNIT TEST - Finished constructing bins");
            Assert.That(engine.observedMzsToUseForIndex.Count > 0);
            Assert.That(engine.baseSequenceToIsotopicDistribution.Count > 0);
            Console.WriteLine("UNIT TEST - Bins are OK");

            for (int i = 0; i < engine.rawFileInfos.Count; i++)
            {
                Console.WriteLine("UNIT TEST - Quantifying file " + (i + 1));
                try
                {
                    Assert.That(engine.Quantify(thermoFile, engine.rawFileInfos[i].fullFilePath));
                }
                catch (AssertionException)
                {
                    Console.WriteLine("UNIT TEST - Could not quantify file \"" + engine.rawFileInfos[i].fullFilePath + "\"");
                }
            }

            //if (engine.mbr)
            //    engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();

            Console.WriteLine("UNIT TEST - Quantifying proteins ");
            engine.QuantifyProteins();

            Console.WriteLine("UNIT TEST - Asserting results");
            Assert.That(engine.SumFeatures(engine.rawFileInfos.SelectMany(p => p.peaksForThisFile), true).Any());
            Assert.That(engine.SumFeatures(engine.rawFileInfos.SelectMany(p => p.peaksForThisFile), false).Any());

            Assert.That(engine.rawFileInfos[0].peaksForThisFile.First().intensity > 0);

            Assert.That(engine.rawFileInfos[0].peaksForThisFile.Count == 1);

            Assert.That(!engine.rawFileInfos[0].peaksForThisFile.First().isMbrFeature);
            Console.WriteLine("UNIT TEST - All passed");
        }
    }
}
