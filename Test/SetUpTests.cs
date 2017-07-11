using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulProteomicsDatabases;


namespace Test
{
    [SetUpFixture]
    public class SetUpTests
    {
        private const string elementsLocation = @"elements.dat";

        [OneTimeSetUp]
        public static void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            Loaders.LoadElements(Path.Combine(TestContext.CurrentContext.TestDirectory, elementsLocation));
        }
    }
}
