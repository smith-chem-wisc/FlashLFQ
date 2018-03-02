using NUnit.Framework;
using System.IO;

namespace Test
{
    [TestFixture]
    internal class Test
    {
        [Test]
        public static void TestFlashLfqExecutable()
        {
            var myDirectory = TestContext.CurrentContext.TestDirectory;
            var pathOfIdentificationFile = Path.Combine(myDirectory, @"SampleFiles\aggregatePSMs_5ppmAroundZero.psmtsv");
            var pathOfMzml = Path.Combine(myDirectory, @"SampleFiles\sliced-mzml.mzML");
            Assert.That(File.Exists(pathOfIdentificationFile));
            Assert.That(File.Exists(pathOfMzml));

            string[] myargs = new string[]
            {
                "--rep " + pathOfIdentificationFile,
                "--idt " + pathOfIdentificationFile,
                "--ppm 5"
            };

            FlashLFQExecutable.FlashLFQExecutable.Main(myargs);
        }
    }
}
