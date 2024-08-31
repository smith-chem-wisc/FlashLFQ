using CMD;
using FlashLFQ;
using IO.MzML;
using MzLibUtil;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Interfaces;
using Util;

namespace Test
{
    [TestFixture]
    internal class LocalFileRunner
    {

        [Test]
        public static void GygiTwoProteome()
        {
            string psmFile = @"D:\GygiTwoProteome_PXD014415\MsConvertmzMLs\MM_ConcatenatedHumanDb_Search\Task1-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\GygiTwoProteome_PXD014415\MsConvertmzMLs\MM_ConcatenatedHumanDb_Search\Task1-SearchTask\AllPeptides.psmtsv";
            var mzmlDirectoy = @"D:\GygiTwoProteome_PXD014415\MsConvertmzMLs\MM_NewPepSearch\Task1-CalibrateTask";


            string outputPath = @"D:\GygiTwoProteome_PXD014415\FlashLFQ_CurRel_PepQ" + "_ConcatenatedDb";

            string[] myargs = new string[]
            {
                "--rep",
                mzmlDirectoy,
                "--idt",
                psmFile,
                "--pep",
                peptideFile,
                "--out",
                outputPath,
                "--donorq",
                "0.001",
                "--pipfdr",
                "0.01",
                "--ppm",
                "10"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            //File.Delete(peaksPath);

            string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            //File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            //File.Delete(proteinsPath);
        }

        [Test]
        public static void GygiTwoProteomeCensored()
        {

            string psmFile = @"D:\GygiTwoProteome_PXD014415\MsConvertmzMLs\MM_ConcatenatedHumanDb_Search_CensoredFiles\Task1-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\GygiTwoProteome_PXD014415\MsConvertmzMLs\MM_ConcatenatedHumanDb_Search_CensoredFiles\Task1-SearchTask\AllPeptides.psmtsv";
            var mzmlDirectoy = @"D:\GygiTwoProteome_PXD014415\MsConvertmzMLs\MM_NewPep_CensoredMzmls";

            string outputPath = @"D:\GygiTwoProteome_PXD014415\CensoredData_FlashLFQ_CurRel_PepQ" + "_ConcatenatedDb";

            string[] myargs = new string[]
            {
                "--rep",
                mzmlDirectoy,
                "--idt",
                psmFile,
                "--pep",
                peptideFile,
                "--out",
                outputPath,
                "--donorq",
                "0.001",
                "--pipfdr",
                "0.01",
                "--ppm",
                "10"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            //File.Delete(peaksPath);

            string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            //File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            //File.Delete(proteinsPath);
        }


        [Test]
        public static void InHouseTwoProteomeHuman()
        {
            string psmFile = @"D:\Human_Ecoli_TwoProteome_60minGradient\RawData\MM_ConcatenatedHumanDb_Search\Task1-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\Human_Ecoli_TwoProteome_60minGradient\RawData\MM_ConcatenatedHumanDb_Search\Task1-SearchTask\AllPeptides.psmtsv"; ;

            var mzmlDirectoy = @"D:\Human_Ecoli_TwoProteome_60minGradient\RawData\MMNewPep_CalSearch\Task1-CalibrateTask";

            string outputPath = @"D:\Human_Ecoli_TwoProteome_60minGradient\Human_FlashLFQ_CurRel_PepQ" + "_ConcatenatedDb";

            string[] myargs = new string[]
            {
                "--rep",
                mzmlDirectoy,
                "--idt",
                psmFile,
                "--pep",
                peptideFile,
                "--out",
                outputPath,
                "--donorq",
                "0.001",
                "--pipfdr",
                "0.01",
                "--ppm",
                "10"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            //File.Delete(peaksPath);

            string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            //File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            //File.Delete(proteinsPath);
        }

        [Test]
        public static void InHouseTwoProteomeHumanCensored()
        {
            string psmFile = @"D:\Human_Ecoli_TwoProteome_60minGradient\RawData\MM_ConcatenatedHumanDb_Search_CensoredFiles\Task1-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\Human_Ecoli_TwoProteome_60minGradient\RawData\MM_ConcatenatedHumanDb_Search_CensoredFiles\Task1-SearchTask\AllPeptides.psmtsv";

            var mzmlDirectoy = @"D:\Human_Ecoli_TwoProteome_60minGradient\RawData\MM_CensoredMzml_8_3_24";

            string outputPath = @"D:\Human_Ecoli_TwoProteome_60minGradient\CensoredHuman_FlashLFQ_CurRel_PepQ" + "_ConcatenatedDb";

            string[] myargs = new string[]
            {
                "--rep",
                mzmlDirectoy,
                "--idt",
                psmFile,
                "--pep",
                peptideFile,
                "--out",
                outputPath,
                "--donorq",
                "0.001",
                "--pipfdr",
                "0.01",
                "--ppm",
                "10"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            //File.Delete(peaksPath);

            string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            //File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            //File.Delete(proteinsPath);
        }

        [Test]
        public static void KellyTwoProteome()
        {
            string psmFile = @"D:\Kelly_TwoProteomeData\MsConvertMzMls\MM_ConcatenatedHumanDb_Search\Task1-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\Kelly_TwoProteomeData\MsConvertMzMls\MM_ConcatenatedHumanDb_Search\Task1-SearchTask\AllPeptides.psmtsv";

            var mzmlDirectoy = @"D:\Kelly_TwoProteomeData\MsConvertMzMls\MetaMorpheusNewPepSearch\Task1-CalibrateTask";

            string outputPath = @"D:\Kelly_TwoProteomeData\FlashLFQ_CurRel_PepQ" + "_ConcatenatedDb";

            string[] myargs = new string[]
            {
                "--rep",
                mzmlDirectoy,
                "--idt",
                psmFile,
                "--pep",
                peptideFile,
                "--out",
                outputPath,
                "--donorq",
                "0.001",
                "--pipfdr",
                "0.01",
                "--ppm",
                "10"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            //File.Delete(peaksPath);

            string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));

            string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            //File.Delete(proteinsPath);
        }

        [Test]
        public static void KellyTwoProteomeCensored()
        {
            string psmFile = @"D:\Kelly_TwoProteomeData\MsConvertMzMls\MM_ConcatenatedHumanDb_Search_CensoredFiles\Task1-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\Kelly_TwoProteomeData\MsConvertMzMls\MM_ConcatenatedHumanDb_Search_CensoredFiles\Task1-SearchTask\AllPeptides.psmtsv";

            var mzmlDirectoy = @"D:\Kelly_TwoProteomeData\MsConvertMzMls\MM_Censored_8_2_24";

            string outputPath = @"D:\Kelly_TwoProteomeData\CensoredData_FlashLFQ_CurRel_PepQ" + "_ConcatenatedDb";

            string[] myargs = new string[]
            {
                "--rep",
                mzmlDirectoy,
                "--idt",
                psmFile,
                "--pep",
                peptideFile,
                "--out",
                outputPath,
                "--donorq",
                "0.001",
                "--pipfdr",
                "0.01",
                "--ppm",
                "10"
            };

            CMD.FlashLfqExecutable.Main(myargs);

            string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            //File.Delete(peaksPath);

            string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            //File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            //File.Delete(proteinsPath);
        }


        [Test]
        [TestCase("02")]
        [TestCase("05")]
        [TestCase("1")]
        public static void TestMetaMorpheusOutputIonStar(string donorQ)
        {
            string psmFile = @"D:\PXD003881_IonStar_SpikeIn\MM_NewPEP_CalSearch\Task2-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\PXD003881_IonStar_SpikeIn\MM_NewPEP_CalSearch\Task2-SearchTask\AllPeptides.psmtsv";

            var mzmlDirectoy = @"D:\PXD003881_IonStar_SpikeIn\MM_NewPEP_CalSearch\Task1-CalibrateTask";

            string outputBase = @"D:\PXD003881_IonStar_SpikeIn\FlashLFQ_330_Normed_Donor" + donorQ + "_NewSearch";

            string outputPath = outputBase + "_Pip1";

            string[] myargs = new string[]
            {
                "--rep",
                mzmlDirectoy,
                "--idt",
                psmFile,
                "--pep",
                peptideFile,
                "--out",
                outputPath,
                "--donorq",
                "0.0" + donorQ,
                "--pipfdr",
                "0.01",
                "--ppm",
                "10",
                "--thr",
                "12",
                "--nor",
                "true"
            };

            CMD.FlashLfqExecutable.Main(myargs);


            string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
            Assert.That(File.Exists(peaksPath));
            //File.Delete(peaksPath);

            string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
            Assert.That(File.Exists(peptidesPath));
            //File.Delete(peptidesPath);

            string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
            Assert.That(File.Exists(proteinsPath));
            //File.Delete(proteinsPath);

            var results = FlashLfqExecutable.Results;

            //PIP PEP Threshold 0.02
            outputPath = outputBase + "_Pip2p5";
            Directory.CreateDirectory(outputPath);
            results.MbrQValueThreshold = 0.025;
            results.ReNormalizeResults();
            results.CalculatePeptideResults(false);
            results.CalculateProteinResultsMedianPolish(false);
            results.WriteResults(
                peaksOutputPath: null,
                modPeptideOutputPath: Path.Combine(outputPath, "QuantifiedPeptides.tsv"),
                    proteinOutputPath: null,
                    bayesianProteinQuantOutput: null,
                    silent: false);
            results.WriteResults(
                peaksOutputPath: null,
                modPeptideOutputPath: null,
                proteinOutputPath: Path.Combine(outputPath, "QuantifiedProteins.tsv"),
                bayesianProteinQuantOutput: null,
                silent: false);

            //PIP PEP Threshold 0.10
            outputPath = outputBase + "_Pip5";
            Directory.CreateDirectory(outputPath);
            results.MbrQValueThreshold = 0.05;
            results.ReNormalizeResults();
            results.CalculatePeptideResults(false);
            results.CalculateProteinResultsMedianPolish(false);
            results.WriteResults(
                peaksOutputPath: null,
                modPeptideOutputPath: Path.Combine(outputPath, "QuantifiedPeptides.tsv"),
                proteinOutputPath: null,
                bayesianProteinQuantOutput: null,
                silent: false);
            results.WriteResults(
                peaksOutputPath: null,
                modPeptideOutputPath: null,
                proteinOutputPath: Path.Combine(outputPath, "QuantifiedProteins.tsv"),
                bayesianProteinQuantOutput: null,
                silent: false);

            // PIP PEP Threshold 1.00
            outputPath = outputBase + "_Pip100";
            Directory.CreateDirectory(outputPath);
            results.MbrQValueThreshold = 1.0;
            results.ReNormalizeResults();
            results.CalculatePeptideResults(false);
            results.CalculateProteinResultsMedianPolish(false);
            results.WriteResults(
                peaksOutputPath: null,
                modPeptideOutputPath: Path.Combine(outputPath, "QuantifiedPeptides.tsv"),
                proteinOutputPath: null,
                bayesianProteinQuantOutput: null,
                silent: false);
            results.WriteResults(
                peaksOutputPath: null,
                modPeptideOutputPath: null,
                proteinOutputPath: Path.Combine(outputPath, "QuantifiedProteins.tsv"),
                bayesianProteinQuantOutput: null,
                silent: false);
        }


    //[Test]
    //[TestCase("02")]
    //[TestCase("1")]
    //[TestCase("5")]
    //public static void TestMetaMorpheusOutputSc()
    //{
    //    string psmFile = @"D:\pepDesc_spikeIn\MM105_CalSearch\Task2-SearchTask\AllPSMs.psmtsv";
    //    string peptideFile = @"D:\pepDesc_spikeIn\MM105_CalSearch\Task2-SearchTask\AllPeptides.psmtsv";

    //    var mzmlDirectoy = @"D:\pepDesc_spikeIn\MM105_CalSearch\Task1-CalibrateTask\mzMLs";

    //    string outputBase = @"D:\pepDesc_spikeIn\FlashLFQ_301_Donor01";

    //    string outputPath = outputBase + "_Pip5";

    //    string[] myargs = new string[]
    //    {
    //        "--rep",
    //        mzmlDirectoy,
    //        "--idt",
    //        psmFile,
    //        "--pep",
    //        peptideFile,
    //        "--out",
    //        outputPath,
    //        "--donorq",
    //        "0.001",
    //        "--pipfdr",
    //        "0.05",
    //        "--ppm",
    //        "10"
    //    };

    //    CMD.FlashLfqExecutable.Main(myargs);

    //    string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
    //    Assert.That(File.Exists(peaksPath));
    //    //File.Delete(peaksPath);

    //    string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
    //    Assert.That(File.Exists(peptidesPath));
    //    //File.Delete(peptidesPath);

    //    string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
    //    Assert.That(File.Exists(proteinsPath));
    //File.Delete(proteinsPath);

    //var results = FlashLfqExecutable.Results;

    ////PIP PEP Threshold 0.01
    //outputPath = outputBase + "_Pip1";
    //Directory.CreateDirectory(outputPath);
    //results.MbrQValueThreshold = 0.01;
    //results.CalculatePeptideResults(false);
    //results.CalculateProteinResultsMedianPolish(false);
    //results.WriteResults(
    //    peaksOutputPath: null,
    //    modPeptideOutputPath: Path.Combine(outputPath, "QuantifiedPeptides.tsv"),
    //        proteinOutputPath: null,
    //        bayesianProteinQuantOutput: null,
    //        silent: false);
    //results.WriteResults(
    //    peaksOutputPath: null,
    //    modPeptideOutputPath: null,
    //    proteinOutputPath: Path.Combine(outputPath, "QuantifiedProteins.tsv"),
    //    bayesianProteinQuantOutput: null,
    //    silent: false);

    ////PIP PEP Threshold 0.10
    //outputPath = outputBase + "_Pip10";
    //Directory.CreateDirectory(outputPath);
    //results.MbrQValueThreshold = 0.1;
    //results.CalculatePeptideResults(false);
    //results.CalculateProteinResultsMedianPolish(false);
    //results.WriteResults(
    //    peaksOutputPath: null,
    //    modPeptideOutputPath: Path.Combine(outputPath, "QuantifiedPeptides.tsv"),
    //    proteinOutputPath: null,
    //    bayesianProteinQuantOutput: null,
    //    silent: false);
    //results.WriteResults(
    //    peaksOutputPath: null,
    //    modPeptideOutputPath: null,
    //    proteinOutputPath: Path.Combine(outputPath, "QuantifiedProteins.tsv"),
    //    bayesianProteinQuantOutput: null,
    //    silent: false);

    //// PIP PEP Threshold 1.00
    //outputPath = outputBase + "_Pip100";
    //Directory.CreateDirectory(outputPath);
    //results.MbrQValueThreshold = 1.0;
    //results.CalculatePeptideResults(false);
    //results.CalculateProteinResultsMedianPolish(false);
    //results.WriteResults(
    //    peaksOutputPath: null,
    //    modPeptideOutputPath: Path.Combine(outputPath, "QuantifiedPeptides.tsv"),
    //    proteinOutputPath: null,
    //    bayesianProteinQuantOutput: null,
    //    silent: false);
    //results.WriteResults(
    //    peaksOutputPath: null,
    //    modPeptideOutputPath: null,
    //    proteinOutputPath: Path.Combine(outputPath, "QuantifiedProteins.tsv"),
    //    bayesianProteinQuantOutput: null,
    //    silent: false);
}

    //[Test]
    //public static void SingleCellFcTest()
    //{
    //    string psmFile = @"D:\SpikeIn_PXD001819\MetaMorpheus105_medium_search\Task1-SearchTask\AllPSMs.psmtsv";
    //    string peptideFile = @"D:\SpikeIn_PXD001819\MetaMorpheus105_medium_search\Task1-SearchTask\AllPeptides.psmtsv";

    //    var mzmlDirectoy = @"D:\SpikeIn_PXD001819\MetaMorpheus105_Cal_Search_Quant\Task1-CalibrateTask\FlashLFQ_Files";

    //    string outputPath = @"D:\SpikeIn_PXD001819\MetaMorpheus105_medium_search\FlashLFQ_209_donor_1_pip_5";

    //    string[] myargs = new string[]
    //    {
    //        "--rep",
    //        mzmlDirectoy,
    //        "--idt",
    //        psmFile,
    //        "--pep",
    //        peptideFile,
    //        "--out",
    //        outputPath,
    //        "--donorq",
    //        "0.01",
    //        "--pipfdr",
    //        "0.02",
    //        "--ppm",
    //        "10"
    //    };

    //    CMD.FlashLfqExecutable.Main(myargs);

    //    string peaksPath = Path.Combine(outputPath, "QuantifiedPeaks.tsv");
    //    Assert.That(File.Exists(peaksPath));
    //    //File.Delete(peaksPath);

    //    string peptidesPath = Path.Combine(outputPath, "QuantifiedPeptides.tsv");
    //    Assert.That(File.Exists(peptidesPath));
    //    //File.Delete(peptidesPath);

    //    string proteinsPath = Path.Combine(outputPath, "QuantifiedProteins.tsv");
    //    Assert.That(File.Exists(proteinsPath));
    //    //File.Delete(proteinsPath);
    //}
}
