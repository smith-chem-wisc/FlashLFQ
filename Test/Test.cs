using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FlashLFQ;
using IO.Thermo;
using System.IO;

namespace Test
{
    [TestFixture]
    class Test
    {
        [Test]
        public static void TestEverything()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            FlashLFQEngine engine = new FlashLFQEngine();

            var path = Environment.CurrentDirectory;

            Assert.That(engine.ParseArgs(new string[] {
                        "--idt " + path + @"\aggregatePSMs_5ppmAroundZero.psmtsv",
                        "--rep " + path,
                        "--ppm 5",
                        "--sil true",
                        "--pau false",
                        "--mbr true",
                        "--sil false",
                        "--pau false" }
                    ));
            engine.globalStopwatch.Start();
            Assert.That(engine.outputFolder != null);
            engine.SetParallelization(1);
            
            Assert.That(engine.ReadPeriodicTable());
            
            Assert.That(engine.ReadIdentificationsFromTSV());

            engine.ConstructBinsFromIdentifications();
            Assert.That(engine.mzBinsTemplate.Count > 0);
            Assert.That(engine.baseSequenceToIsotopicDistribution.Count > 0);
            
            for (int i = 0; i < engine.filePaths.Length; i++)
                Assert.That(engine.Quantify(null, engine.filePaths[i]));

            //if (engine.mbr)
            //    engine.RetentionTimeCalibrationAndErrorCheckMatchedFeatures();

            engine.QuantifyProteins();

            Assert.That(engine.SumFeatures(engine.allFeaturesByFile.SelectMany(p => p)).Any());

            Assert.That(engine.allFeaturesByFile[0].First().intensity > 0);
            Assert.That(engine.allFeaturesByFile[1].First().intensity > 0);

            Assert.That(!engine.allFeaturesByFile[0].First().isMbrFeature);
            Assert.That(!engine.allFeaturesByFile[1].First().isMbrFeature);
        }
    }
}
