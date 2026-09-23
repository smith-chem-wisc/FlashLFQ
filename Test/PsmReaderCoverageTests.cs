using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FlashLFQ;
using MassSpectrometry;
using NUnit.Framework;
using Util;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace Test
{
    /// <summary>
    /// Coverage for the read-robustness changes in PsmReader: fail-fast on an unrecognized header,
    /// capped per-line error logging, and non-throwing q-value parsing. These exercise the legacy
    /// reader path directly, since ReadPsms prefers the mzLib QuantifiableResultFile reader for
    /// well-formed files.
    /// </summary>
    [TestFixture]
    public class PsmReaderCoverageTests
    {
        private static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

        private static string SampleMmPsmPath =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", "MetaMorpheus", "AllPSMs.psmtsv");

        private static object InvokeGetFileType(PsmReader reader, string header, bool usePepQValue)
        {
            var mi = typeof(PsmReader).GetMethod("GetFileTypeFromHeader", Priv);
            return mi.Invoke(reader, new object[] { header, usePepQValue });
        }

        [Test]
        public void GetFileTypeFromHeader_RecognizesMetaMorpheus()
        {
            string header = File.ReadLines(SampleMmPsmPath).First();
            var reader = new PsmReader();
            Assert.AreEqual("MetaMorpheus", InvokeGetFileType(reader, header, false).ToString());
            Assert.AreEqual("MetaMorpheus", InvokeGetFileType(reader, header, true).ToString());
        }

        [Test]
        public void GetFileTypeFromHeader_ReturnsUnknownForUnrecognizedHeader()
        {
            var reader = new PsmReader();
            var result = InvokeGetFileType(reader, "foo\tbar\tbaz", false);
            Assert.AreEqual("Unknown", result.ToString());
        }

        [Test]
        public void ReadPsms_FailsFastOnUnrecognizedHeader()
        {
            // A file the mzLib quantifiable reader can't handle falls back to the legacy reader; if its
            // header also matches no supported format, ReadPsms should fail fast with a clear message
            // rather than attempting to parse every row (which previously threw once per row and hung the GUI).
            // An unsupported extension guarantees the mzLib path is skipped so the legacy fail-fast is reached.
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "unrecognized_header.dat");
            File.WriteAllLines(path, new[] { "colA\tcolB\tcolC", "1\t2\t3", "4\t5\t6" });

            var reader = new PsmReader();
            var ex = Assert.Catch(() => reader.ReadPsms(path, silent: true, new List<SpectraFileInfo>()));
            Assert.IsNotNull(ex, "expected ReadPsms to throw for an unrecognized header");
            Assert.IsTrue(ex.Message.Contains("not recognized") || (ex.InnerException?.Message.Contains("not recognized") ?? false),
                "expected a 'format not recognized' message but got: " + ex.Message);
        }

        [Test]
        public void LogLineReadError_CapsLoggingAndSummarizes()
        {
            var reader = new PsmReader();
            var mi = typeof(PsmReader).GetMethod("LogLineReadError", Priv);
            const int max = 50;

            var original = Console.Out;
            var captured = new StringWriter();
            Console.SetOut(captured);
            try
            {
                // One boxed args array so the `ref` counter accumulates across calls.
                object[] args = { false, new Exception("boom"), 0, max };
                for (int i = 0; i < max + 10; i++)
                {
                    mi.Invoke(reader, args);
                }
            }
            finally { Console.SetOut(original); }

            string log = captured.ToString();
            int problemLines = log.Split('\n').Count(l => l.Contains("Problem reading line"));
            int summaryLines = log.Split('\n').Count(l => l.Contains("further messages are suppressed"));

            Assert.AreEqual(max, problemLines, "should log exactly the cap of per-line errors");
            Assert.AreEqual(1, summaryLines, "should emit the suppression summary exactly once");
        }

        [Test]
        public void LogLineReadError_SilentWritesNothing()
        {
            var reader = new PsmReader();
            var mi = typeof(PsmReader).GetMethod("LogLineReadError", Priv);

            var original = Console.Out;
            var captured = new StringWriter();
            Console.SetOut(captured);
            try
            {
                object[] args = { true, new Exception("boom"), 0, 50 };
                mi.Invoke(reader, args);
            }
            finally { Console.SetOut(original); }

            Assert.IsEmpty(captured.ToString().Trim());
        }

        [Test]
        public void GetIdentification_QValueParsing_SkipsBadAndAboveThresholdRows()
        {
            // Read the sample header + one real data row so all column indices and field values are valid.
            var lines = File.ReadLines(SampleMmPsmPath).Take(2).ToArray();
            string header = lines[0];
            string goodRow = lines[1];
            int qNotchCol = Array.FindIndex(header.Split('\t'), h => h == "QValue Notch");

            var reader = new PsmReader();
            var fileType = InvokeGetFileType(reader, header, false); // sets column indices, returns MetaMorpheus

            // Initialize the private state ReadPsms would normally set up.
            typeof(PsmReader).GetField("_modSequenceToMonoMass", Priv)
                .SetValue(reader, new Dictionary<string, double>());
            typeof(PsmReader).GetField("allProteinGroups", Priv)
                .SetValue(reader, new Dictionary<string, ProteinGroup>());

            string mzml = Path.Combine(TestContext.CurrentContext.TestDirectory, "SampleFiles", "SmallCalibratible_Yeast.mzML");
            var sfi = new SpectraFileInfo(mzml, "A", 1, 1, 1);
            var rawDict = new Dictionary<string, SpectraFileInfo> { { sfi.FilenameWithoutExtension, sfi } };

            var getId = typeof(PsmReader).GetMethod("GetIdentification", Priv);

            object CallGetId(string row) =>
                getId.Invoke(reader, new object[] { row, true, rawDict, fileType, 0.01 });

            // Valid row with a passing q-value -> an identification is produced.
            Assert.IsNotNull(CallGetId(goodRow), "a valid PSM row should produce an identification");

            // Non-numeric q-value -> TryParse fails, row is skipped (returns null) rather than throwing.
            var badCols = goodRow.Split('\t');
            badCols[qNotchCol] = "notanumber";
            Assert.IsNull(CallGetId(string.Join('\t', badCols)), "a non-numeric q-value should be skipped");

            // q-value above the threshold -> row is filtered out.
            var highCols = goodRow.Split('\t');
            highCols[qNotchCol] = "0.5";
            Assert.IsNull(CallGetId(string.Join('\t', highCols)), "a q-value above threshold should be skipped");
        }
    }
}
