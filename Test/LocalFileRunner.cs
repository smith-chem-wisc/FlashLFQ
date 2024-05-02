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
    internal class LocalFileRunner
    {
        [Test]
        public static void TestMetaMorpheusOutput()
        {
            string psmFile = @"D:\SpikeIn_PXD001819\MetaMorpheus105_medium_search\Task1-SearchTask\AllPSMs.psmtsv";
            string peptideFile = @"D:\SpikeIn_PXD001819\MetaMorpheus105_medium_search\Task1-SearchTask\AllPeptides.psmtsv";

            var mzmlDirectoy = @"D:\SpikeIn_PXD001819\MetaMorpheus105_Cal_Search_Quant\Task1-CalibrateTask\FlashLFQ_Files";

            string outputPath = @"D:\SpikeIn_PXD001819\MetaMorpheus105_medium_search\FlashLFQ_209_donor_02_pip_2";

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
                "0.01",
                "--pipfdr",
                "0.02",
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
    }
}
