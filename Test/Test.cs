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
            var myDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles");
            var pathOfIdentificationFile = Path.Combine(myDirectory, @"aggregatePSMs_5ppmAroundZero.psmtsv");
            var pathOfMzml = Path.Combine(myDirectory, @"sliced-mzml.mzML");
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

            FlashLFQExecutable.FlashLFQExecutable.Main(myargs);
        }
    }
}
