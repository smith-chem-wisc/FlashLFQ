using Chemistry;
using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using Proteomics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UsefulProteomicsDatabases;

namespace Engine
{
    public enum IdentificationFileType { MetaMorpheus, Morpheus, MaxQuant, TDPortal };

    public class FlashLFQEngine
    {
        // file info stuff
        public string identificationsFilePath { get; private set; }
        public string[] filePaths { get; private set; }
        public string[] analysisSummaryPerFile { get; private set; }
        public string outputFolder;

        // structures used in the FlashLFQ program
        private List<FlashLFQIdentification> allIdentifications;
        public List<FlashLFQFeature>[] allFeaturesByFile { get; private set; }
        private Dictionary<double, List<FlashLFQMzBinElement>> mzBinsTemplate;
        private Dictionary<string, List<KeyValuePair<double, double>>> baseSequenceToIsotopicDistribution;
        private Dictionary<string, FlashLFQProteinGroup> pepToProteinGroupDictionary;
        private string[] header;
        public Stopwatch globalStopwatch;
        public Stopwatch fileLocalStopwatch;

        // settings
        public bool silent { get; private set; }
        public bool pause { get; private set; }
        public int maxDegreesOfParallelism { get; private set; }
        public IEnumerable<int> chargeStates { get; private set; }
        public double peakfindingPpmTolerance { get; private set; }
        public double ppmTolerance { get; private set; }
        public double rtTol { get; private set; }
        public double isotopePpmTolerance { get; private set; }
        public bool integrate { get; private set; }
        public int missedScansAllowed { get; private set; }
        public int numIsotopesRequired { get; private set; }
        public double mbrRtWindow { get; private set; }
        public double mbrppmTolerance { get; private set; }
        public bool errorCheckAmbiguousMatches { get; private set; }
        public bool mbr { get; private set; }
        public bool idSpecificChargeState { get; private set; }
        public double qValueCutoff { get; private set; }
        public IdentificationFileType identificationFileType { get; private set; }

        public FlashLFQEngine()
        {
            globalStopwatch = new Stopwatch();
            fileLocalStopwatch = new Stopwatch();
            allIdentifications = new List<FlashLFQIdentification>();
            pepToProteinGroupDictionary = new Dictionary<string, FlashLFQProteinGroup>();
            chargeStates = new List<int>();

            // default parameters
            ppmTolerance = 10.0;
            peakfindingPpmTolerance = 20.0;
            isotopePpmTolerance = 5.0;
            mbr = false;
            mbrRtWindow = 1.5;
            mbrppmTolerance = 5.0;
            integrate = false;
            missedScansAllowed = 1;
            numIsotopesRequired = 2;
            rtTol = 5.0;
            silent = false;
            pause = true;
            errorCheckAmbiguousMatches = true;
            maxDegreesOfParallelism = -1;
            idSpecificChargeState = false;
            qValueCutoff = 0.01;
        }

        public bool ParseArgs(string[] args)
        {
            string[] validArgs = new string[] {
                "--idt [string|identification file path (TSV format)]",
                "--raw [string|MS data file (.raw or .mzml)]",
                "--rep [string|directory containing MS data files]",
                "--ppm [double|ppm tolerance]",
                "--iso [double|isotopic distribution tolerance in ppm]",
                "--sil [bool|silent mode]",
                "--pau [bool|pause at end of run]",
                "--int [bool|integrate features]",
                "--sum [bool|sum features in a run]",
                "--mbr [bool|match between runs]",
                "--chg [bool|use only precursor charge state]"
            };
            var newargs = string.Join("", args).Split(new[] { "--" }, StringSplitOptions.None);

            for (int i = 0; i < newargs.Length; i++)
                newargs[i] = newargs[i].Trim();

            newargs = newargs.Where(a => !a.Equals("")).ToArray();
            if (newargs.Length == 0)
            {
                Console.WriteLine("Accepted args are: " + string.Join(" ", validArgs) + "\n");
                Console.ReadKey();
                return false;
            }

            foreach (var arg in newargs)
            {
                try
                {
                    string flag = arg.Substring(0, 3);

                    switch (flag)
                    {
                        case ("idt"): identificationsFilePath = arg.Substring(3); break;
                        case ("raw"): filePaths = new string[] { arg.Substring(3) }; break;
                        case ("rep"):
                            string newArg = arg;
                            if (newArg.EndsWith("\""))
                                newArg = arg.Substring(0, arg.Length - 1);
                            filePaths = Directory.GetFiles(newArg.Substring(3)).Where(f => f.Substring(f.IndexOf('.')).ToUpper().Equals(".RAW") || f.Substring(f.IndexOf('.')).ToUpper().Equals(".MZML")).ToArray(); break;
                        case ("ppm"): ppmTolerance = double.Parse(arg.Substring(3)); break;
                        case ("iso"): isotopePpmTolerance = double.Parse(arg.Substring(3)); break;
                        case ("sil"): silent = Boolean.Parse(arg.Substring(3)); break;
                        case ("pau"): pause = Boolean.Parse(arg.Substring(3)); break;
                        case ("int"): integrate = Boolean.Parse(arg.Substring(3)); break;
                        case ("mbr"): mbr = Boolean.Parse(arg.Substring(3)); break;
                        case ("chg"): idSpecificChargeState = Boolean.Parse(arg.Substring(3)); break;
                        case ("nis"): numIsotopesRequired = int.Parse(arg.Substring(3)); break;
                        default:
                            if (!silent)
                            {
                                Console.WriteLine("Not a known argument: \"" + flag + "\"\n");
                                Console.WriteLine("Accepted args are: " + string.Join(" ", validArgs) + "\n");
                                Console.WriteLine("Press any key to exit");
                                Console.ReadKey();
                            }
                            return false;
                    }
                }
                catch (Exception)
                {
                    if (!silent)
                    {
                        Console.WriteLine("Can't parse argument \"" + arg + "\"\n");
                        Console.WriteLine("Accepted args are: " + string.Join(" ", validArgs) + "\n");
                        Console.WriteLine("Press any key to exit");
                        Console.ReadKey();
                    }
                    return false;
                }
            }

            if (filePaths != null && filePaths.Length == 0)
            {
                if (!silent)
                {
                    Console.WriteLine("Couldn't find any MS data files at the location specified\n");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                }
                return false;
            }

            if (outputFolder == null)
                outputFolder = identificationsFilePath.Substring(0, identificationsFilePath.Length - (identificationsFilePath.Length - identificationsFilePath.IndexOf('.')));

            analysisSummaryPerFile = new string[filePaths.Length];
            allFeaturesByFile = new List<FlashLFQFeature>[filePaths.Length];
            return true;
        }

        public void PassFilePaths(string[] paths)
        {
            filePaths = paths.Distinct().ToArray();
            analysisSummaryPerFile = new string[filePaths.Length];
            allFeaturesByFile = new List<FlashLFQFeature>[filePaths.Length];
        }

        public bool ReadPeriodicTable()
        {
            try
            {
                Loaders.LoadElements(@"elements.dat");
            }
            catch (Exception)
            {
                if (!silent)
                {
                    Console.WriteLine("\nCan't read periodic table file\n");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                }
                return false;
            }
            return true;
        }

        public bool ReadIdentificationsFromTSV()
        {
            int fileNameCol = -1;
            int baseSequCol = -1;
            int fullSequCol = -1;
            int monoMassCol = -1;
            int msmsRetnCol = -1;
            int chargeStCol = -1;
            int protNameCol = -1;
            int decoyCol = -1;
            int qValueCol = -1;

            // read identification file
            if (!silent)
                Console.WriteLine("Opening identification file " + identificationsFilePath);
            string[] tsv;

            try
            {
                tsv = File.ReadAllLines(identificationsFilePath);
            }
            catch (FileNotFoundException)
            {
                if (!silent)
                {
                    Console.WriteLine("\nCan't find identification file\n");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                }
                return false;
            }
            catch (FileLoadException)
            {
                if (!silent)
                {
                    Console.WriteLine("\nCan't read identification file\n");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                }
                return false;
            }
            catch (Exception e)
            {
                if (!silent)
                {
                    Console.WriteLine("\nError reading identification file\n");
                    Console.WriteLine(e.Message + "\nPress any key to exit");
                    Console.ReadKey();
                }
                return false;
            }

            int lineCount = 0;
            foreach (var line in tsv)
            {
                lineCount++;
                var delimiters = new char[] { '\t' };

                if (lineCount == 1)
                {
                    header = line.Split(delimiters);

                    // MetaMorpheus MS/MS input
                    if (header.Contains("File Name")
                        && header.Contains("Base Sequence")
                        && header.Contains("Full Sequence")
                        && header.Contains("Peptide Monoisotopic Mass")
                        && header.Contains("Scan Retention Time")
                        && header.Contains("Precursor Charge")
                        && header.Contains("Protein Accession")
                        && header.Contains("Decoy/Contaminant/Target")
                        && header.Contains("QValue"))
                    {
                        fileNameCol = Array.IndexOf(header, "File Name");
                        baseSequCol = Array.IndexOf(header, "Base Sequence");
                        fullSequCol = Array.IndexOf(header, "Full Sequence");
                        monoMassCol = Array.IndexOf(header, "Peptide Monoisotopic Mass");
                        msmsRetnCol = Array.IndexOf(header, "Scan Retention Time");
                        chargeStCol = Array.IndexOf(header, "Precursor Charge");
                        protNameCol = Array.IndexOf(header, "Protein Accession");
                        decoyCol = Array.IndexOf(header, "Decoy/Contaminant/Target");
                        qValueCol = Array.IndexOf(header, "QValue");
                        identificationFileType = IdentificationFileType.MetaMorpheus;
                    }

                    // Morpheus MS/MS input
                    else if (header.Contains("Filename")
                        && header.Contains("Base Peptide Sequence")
                        && header.Contains("Peptide Sequence")
                        && header.Contains("Theoretical Mass (Da)")
                        && header.Contains("Retention Time (minutes)")
                        && header.Contains("Precursor Charge")
                        && header.Contains("Protein Description")
                        && header.Contains("Decoy?")
                        && header.Contains("Q-Value (%)"))
                    {
                        fileNameCol = Array.IndexOf(header, "Filename");
                        baseSequCol = Array.IndexOf(header, "Base Peptide Sequence");
                        fullSequCol = Array.IndexOf(header, "Peptide Sequence");
                        monoMassCol = Array.IndexOf(header, "Theoretical Mass (Da)");
                        msmsRetnCol = Array.IndexOf(header, "Retention Time (minutes)");
                        chargeStCol = Array.IndexOf(header, "Precursor Charge");
                        protNameCol = Array.IndexOf(header, "Protein Description");
                        decoyCol = Array.IndexOf(header, "Decoy?");
                        qValueCol = Array.IndexOf(header, "Q-Value (%)");
                        identificationFileType = IdentificationFileType.Morpheus;
                    }

                    // MaxQuant MS/MS input
                    else if (header.Contains("Raw file")
                        && header.Contains("Sequence")
                        && header.Contains("Modified sequence")
                        && header.Contains("Mass")
                        && header.Contains("Retention time")
                        && header.Contains("Charge")
                        && header.Contains("Proteins"))
                    {
                        fileNameCol = Array.IndexOf(header, "Raw file");
                        baseSequCol = Array.IndexOf(header, "Sequence");
                        fullSequCol = Array.IndexOf(header, "Modified sequence");
                        monoMassCol = Array.IndexOf(header, "Mass");
                        msmsRetnCol = Array.IndexOf(header, "Retention time");
                        chargeStCol = Array.IndexOf(header, "Charge");
                        protNameCol = Array.IndexOf(header, "Proteins");
                        identificationFileType = IdentificationFileType.MaxQuant;
                    }

                    // TDPortal Input
                    if (header.Contains("File Name")
                        && header.Contains("Sequence")
                        && header.Contains("Modifications")
                        && header.Contains("Monoisotopic Mass")
                        && header.Contains("RetentionTime")
                        && header.Contains("Accession")
                        && header.Contains("% Cleavages"))
                    {
                        fileNameCol = Array.IndexOf(header, "File Name");
                        baseSequCol = Array.IndexOf(header, "Sequence");
                        fullSequCol = Array.IndexOf(header, "Modifications");
                        monoMassCol = Array.IndexOf(header, "Monoisotopic Mass");
                        msmsRetnCol = Array.IndexOf(header, "RetentionTime");
                        protNameCol = Array.IndexOf(header, "Accession");
                        identificationFileType = IdentificationFileType.TDPortal;
                    }

                    // other search engines

                    // can't parse file
                    if (fileNameCol == -1)
                    {
                        if (!silent)
                        {
                            Console.WriteLine("Identification file is improperly formatted");
                            Console.ReadKey();
                        }
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        var param = line.Split(delimiters);

                        string fileName = param[fileNameCol];
                        string BaseSequence = param[baseSequCol];
                        string ModSequence = param[fullSequCol];
                        if (identificationFileType == IdentificationFileType.TDPortal)
                            ModSequence = BaseSequence + ModSequence;
                        double monoisotopicMass = double.Parse(param[monoMassCol]);
                        double ms2RetentionTime = double.Parse(param[msmsRetnCol]);

                        int chargeState;
                        if (identificationFileType == IdentificationFileType.TDPortal)
                        {
                            chargeState = 1;
                        }
                        else
                            chargeState = int.Parse(param[chargeStCol]);

                        var ident = new FlashLFQIdentification(Path.GetFileNameWithoutExtension(fileName), BaseSequence, ModSequence, monoisotopicMass, ms2RetentionTime, chargeState);
                        allIdentifications.Add(ident);

                        FlashLFQProteinGroup pg;
                        if (pepToProteinGroupDictionary.TryGetValue(param[protNameCol], out pg))
                            ident.proteinGroup = pg;
                        else
                        {
                            pg = new FlashLFQProteinGroup(param[protNameCol]);
                            pepToProteinGroupDictionary.Add(param[protNameCol], pg);
                            ident.proteinGroup = pg;
                        }
                    }
                    catch (Exception)
                    {
                        if (!silent)
                        {
                            Console.WriteLine("Problem reading line " + lineCount + " of the TSV file");
                            Console.ReadKey();
                        }
                        return false;
                    }
                }
            }

            if (identificationFileType == IdentificationFileType.TDPortal)
            {
                var idsGroupedByFile = allIdentifications.GroupBy(p => p.fileName);
                var idFileNames = idsGroupedByFile.Select(p => p.Key);
                string[] fileNames = new string[filePaths.Length];
                for (int i = 0; i < filePaths.Length; i++)
                {
                    fileNames[i] = Path.GetFileNameWithoutExtension(filePaths[i]);
                }

                foreach (var fileName in idFileNames)
                {
                    int fileIndex = Array.IndexOf(fileNames, fileName);
                    if (fileIndex == -1)
                        continue;
                    IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file = OpenDataFile(fileIndex);
                    var identificationsForThisFile = allIdentifications.Where(p => Path.GetFileNameWithoutExtension(p.fileName) == Path.GetFileNameWithoutExtension(fileName));

                    foreach (var identification in identificationsForThisFile)
                    {
                        int scanNum = file.GetClosestOneBasedSpectrumNumber(identification.ms2RetentionTime);
                        var scan = file.GetOneBasedScan(scanNum) as IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>;
                        if (scan != null)
                        {
                            identification.chargeState = (int)(identification.monoisotopicMass / scan.SelectedIonMZ);
                        }
                    }
                }
            }

            return true;
        }

        public bool Quantify(IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file, string filePath)
        {
            if (filePaths == null)
                return false;

            fileLocalStopwatch.Restart();

            // construct bins
            var localBins = ConstructLocalBins();

            // open raw file
            int i = Array.IndexOf(filePaths, filePath);
            if (i < 0)
                return false;
            var currentDataFile = file;
            if (currentDataFile == null)
                currentDataFile = OpenDataFile(i);
            if (currentDataFile == null)
                return false;

            // fill bins with peaks from the raw file
            var ms1ScanNumbers = FillBinsWithPeaks(localBins, currentDataFile);

            // quantify features using this file's IDs first
            allFeaturesByFile[i] = MainFileSearch(Path.GetFileNameWithoutExtension(filePath), localBins, ms1ScanNumbers, currentDataFile);

            // find unidentified features based on other files' identification results (MBR)
            if (mbr)
                MatchBetweenRuns(Path.GetFileNameWithoutExtension(filePath), localBins, allFeaturesByFile[i]);

            // error checking function
            // handles features with multiple identifying scans, and
            // also handles scans that are associated with more than one feature
            RunErrorChecking(allFeaturesByFile[i]);

            foreach (var feature in allFeaturesByFile[i])
                foreach (var cluster in feature.isotopeClusters)
                    cluster.peakWithScan.Compress();

            if (!silent)
                Console.WriteLine("Finished " + Path.GetFileNameWithoutExtension(filePath));

            analysisSummaryPerFile[i] = "File analysis time = " + fileLocalStopwatch.Elapsed.ToString();
            return true;
        }

        public bool WriteResults(string baseFileName, bool writePeaks, bool writePeptides, bool writeProteins)
        {
            if (!silent)
                Console.WriteLine("Writing results");
            try
            {
                var allFeatures = allFeaturesByFile.SelectMany(p => p.Select(v => v));

                // write features
                List<string> featureOutput = new List<string> { FlashLFQFeature.TabSeparatedHeader };
                featureOutput = featureOutput.Concat(allFeatures.Select(v => v.ToString())).ToList();
                if (writePeaks)
                    File.WriteAllLines(outputFolder + baseFileName + "QuantifiedPeaks.tsv", featureOutput);

                // write baseseq groups
                var peptides = SumFeatures(allFeatures);
                List<string> baseSeqOutput = new List<string> { FlashLFQSummedFeatureGroup.TabSeparatedHeader };
                baseSeqOutput = baseSeqOutput.Concat(peptides.Select(p => p.ToString())).ToList();
                if (writePeptides)
                    File.WriteAllLines(outputFolder + baseFileName + "QuantifiedPeptides.tsv", baseSeqOutput);

                // write protein results
                var proteinGroups = allFeatures.Select(p => p.identifyingScans.First().proteinGroup).Where(v => v.intensitiesByFile != null).Distinct().OrderBy(p => p.proteinGroupName);
                List<string> proteinOutput = new List<string> { string.Join("\t", new string[] { "test" }) };
                proteinOutput = proteinOutput.Concat(proteinGroups.Select(v => v.ToString())).ToList();
                if (writeProteins)
                    File.WriteAllLines(outputFolder + baseFileName + "QuantifiedProteins.tsv", proteinOutput);

                //write log
                List<string> logOutput = new List<string>()
                {
                    "Analysis Finish DateTime = " + DateTime.Now.ToString(),
                    "Total Analysis Time = " + globalStopwatch.Elapsed.ToString(),
                    "peakfindingPpmTolerance = " + peakfindingPpmTolerance,
                    "missedScansAllowed = " + missedScansAllowed,
                    "ppmTolerance = " + ppmTolerance,
                    "isotopePpmTolerance = " + isotopePpmTolerance,
                    "numIsotopesRequired = " + numIsotopesRequired,
                    "rtTol = " + rtTol,
                    "integrate = " + integrate,
                    "idSpecificChargeState = " + idSpecificChargeState,
                    "maxDegreesOfParallelism = " + maxDegreesOfParallelism,
                    "mbr = " + mbr,
                    "mbrppmTolerance = " + mbrppmTolerance,
                    "mbrppmTolerance = " + mbrppmTolerance,
                    "mbrRtWindow = " + mbrRtWindow,
                    "errorCheckAmbiguousMatches = " + errorCheckAmbiguousMatches,
                    ""
                };

                for (int i = 0; i < filePaths.Length; i++)
                {
                    logOutput.Add("Analysis summary for: " + filePaths[i]);
                    logOutput.Add("\t" + analysisSummaryPerFile[i] + "\n");
                }

                File.WriteAllLines(outputFolder + baseFileName + "Log.txt", logOutput);
            }
            catch (Exception e)
            {
                if (!silent)
                {
                    Console.WriteLine("Unable to write results file to " + outputFolder);
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Press any key to continue\n");
                    Console.ReadKey();
                }
                return false;
            }

            return true;
        }

        public void RetentionTimeCalibrationAndErrorCheckMatchedFeatures()
        {
            if (!silent)
                Console.WriteLine("Running retention time calibration");

            var allFeatures = allFeaturesByFile.SelectMany(p => p);
            var allAmbiguousFeatures = allFeatures.Where(p => p.numIdentificationsByFullSeq > 1).ToList();
            var ambiguousFeatureSeqs = new HashSet<string>(allAmbiguousFeatures.SelectMany(p => p.identifyingScans.Select(v => v.FullSequence)));

            foreach (var feature in allFeatures)
                if (ambiguousFeatureSeqs.Contains(feature.identifyingScans.First().FullSequence))
                    allAmbiguousFeatures.Add(feature);

            var unambiguousPeaksGroupedByFile = allFeatures.Except(allAmbiguousFeatures).Where(v => v.apexPeak != null).GroupBy(p => p.fileName);

            foreach (var file in unambiguousPeaksGroupedByFile)
            {
                Dictionary<string, FlashLFQFeature> pepToBestFeatureForThisFile = new Dictionary<string, FlashLFQFeature>();
                foreach (var testPeak in file)
                {
                    FlashLFQFeature currentBestPeak;
                    if (pepToBestFeatureForThisFile.TryGetValue(testPeak.identifyingScans.First().FullSequence, out currentBestPeak))
                    {
                        if (currentBestPeak.intensity > testPeak.intensity)
                            pepToBestFeatureForThisFile[testPeak.identifyingScans.First().FullSequence] = testPeak;
                    }
                    else
                        pepToBestFeatureForThisFile.Add(testPeak.identifyingScans.First().FullSequence, testPeak);
                }

                foreach (var otherFile in unambiguousPeaksGroupedByFile)
                {
                    if (otherFile.Key.Equals(file.Key))
                        continue;

                    var featuresInCommon = otherFile.Where(p => pepToBestFeatureForThisFile.ContainsKey(p.identifyingScans.First().FullSequence));

                    Dictionary<string, FlashLFQFeature> pepToBestFeatureForOtherFile = new Dictionary<string, FlashLFQFeature>();
                    foreach (var testPeak in featuresInCommon)
                    {
                        FlashLFQFeature currentBestPeak;
                        if (pepToBestFeatureForOtherFile.TryGetValue(testPeak.identifyingScans.First().FullSequence, out currentBestPeak))
                        {
                            if (currentBestPeak.intensity > testPeak.intensity)
                                pepToBestFeatureForOtherFile[testPeak.identifyingScans.First().FullSequence] = testPeak;
                        }
                        else
                            pepToBestFeatureForOtherFile.Add(testPeak.identifyingScans.First().FullSequence, testPeak);
                    }

                    Dictionary<string, Tuple<double, double>> rtCalPoints = new Dictionary<string, Tuple<double, double>>();

                    foreach (var kvp in pepToBestFeatureForOtherFile)
                        rtCalPoints.Add(kvp.Key, new Tuple<double, double>(pepToBestFeatureForThisFile[kvp.Key].apexPeak.peakWithScan.retentionTime, kvp.Value.apexPeak.peakWithScan.retentionTime));

                    var someDoubles = rtCalPoints.Select(p => (p.Value.Item1 - p.Value.Item2));
                    double average = someDoubles.Average();
                    double sumOfSquaresOfDifferences = someDoubles.Select(val => (val - average) * (val - average)).Sum();
                    double sd = Math.Sqrt(sumOfSquaresOfDifferences / (someDoubles.Count() - 1));

                    while (sd > 1.0)
                    {
                        var pointsToRemove = rtCalPoints.Where(p => p.Value.Item1 - p.Value.Item2 > average + sd || p.Value.Item1 - p.Value.Item2 < average - sd).ToList();
                        foreach (var point in pointsToRemove)
                            rtCalPoints.Remove(point.Key);

                        someDoubles = rtCalPoints.Select(p => (p.Value.Item1 - p.Value.Item2));
                        average = someDoubles.Average();
                        sumOfSquaresOfDifferences = someDoubles.Select(val => (val - average) * (val - average)).Sum();
                        sd = Math.Sqrt(sumOfSquaresOfDifferences / (someDoubles.Count() - 1));
                    }

                    List<string> output = new List<string>();
                    foreach (var point in rtCalPoints)
                    {
                        output.Add("" + point.Key + "\t" + point.Value.Item1 + "\t" + point.Value.Item2 + "\t" + (point.Value.Item1 - point.Value.Item2));
                    }

                    File.WriteAllLines(outputFolder + "RTCal.tsv", output);
                }
            }
        }

        public void QuantifyProteins()
        {
            if (!silent)
                Console.WriteLine("Quantifying proteins");
            var fileNames = filePaths.Select(p => Path.GetFileNameWithoutExtension(p)).ToList();

            var allFeatures = allFeaturesByFile.SelectMany(p => p);
            var allAmbiguousFeatures = allFeatures.Where(p => p.numIdentificationsByBaseSeq > 1).ToList();
            var ambiguousFeatureSeqs = new HashSet<string>(allAmbiguousFeatures.SelectMany(p => p.identifyingScans.Select(v => v.BaseSequence)));

            foreach (var feature in allFeatures)
            {
                if (ambiguousFeatureSeqs.Contains(feature.identifyingScans.First().BaseSequence))
                    allAmbiguousFeatures.Add(feature);
            }

            var allUnambiguousFeatures = allFeatures.Except(allAmbiguousFeatures);
            var featuresGroupedByProtein = allUnambiguousFeatures.GroupBy(v => v.identifyingScans.First().proteinGroup);

            foreach (var proteinFeatures in featuresGroupedByProtein)
            {
                var featuresByFile = proteinFeatures.GroupBy(p => p.fileName);
                var pepBaseSeqs = proteinFeatures.Select(p => p.identifyingScans.First().BaseSequence).Distinct().ToList();

                // construct empty peptide/file array for this protein
                List<FlashLFQFeature>[,] temp = new List<FlashLFQFeature>[filePaths.Length, pepBaseSeqs.Count];
                for (int i = 0; i < temp.GetLength(0); i++)
                    for (int j = 0; j < temp.GetLength(1); j++)
                        temp[i, j] = new List<FlashLFQFeature>();

                // populate array
                foreach (var file in featuresByFile)
                {
                    int fileIndex = fileNames.IndexOf(file.Key);
                    foreach (var feature in file)
                        temp[fileIndex, pepBaseSeqs.IndexOf(feature.identifyingScans.First().BaseSequence)].Add(feature);
                }

                proteinFeatures.Key.intensitiesByFile = new double[fileNames.Count];
                for (int i = 0; i < fileNames.Count; i++)
                {
                    for (int j = 0; j < pepBaseSeqs.Count; j++)
                        proteinFeatures.Key.intensitiesByFile[i] += temp[i, j].Select(p => (p.intensity / p.numIdentificationsByFullSeq)).Sum();
                }
            }
        }

        public void AddIdentification(string fileName, string BaseSequence, string FullSequence, double monoisotopicMass, double ms2RetentionTime, int chargeState, string proteinGroupName)
        {
            var ident = new FlashLFQIdentification(fileName, BaseSequence, FullSequence, monoisotopicMass, ms2RetentionTime, chargeState);

            FlashLFQProteinGroup pg;
            if (pepToProteinGroupDictionary.TryGetValue(proteinGroupName, out pg))
                ident.proteinGroup = pg;
            else
            {
                pg = new FlashLFQProteinGroup(proteinGroupName);
                pepToProteinGroupDictionary.Add(proteinGroupName, pg);
                ident.proteinGroup = pg;
            }

            allIdentifications.Add(ident);
        }

        private IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> OpenDataFile(int fileIndex)
        {
            var massSpecFileFormat = filePaths[fileIndex].Substring(filePaths[fileIndex].IndexOf('.')).ToUpper();
            IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file = null;

            // read mass spec file
            if (!silent)
                Console.WriteLine("Opening " + filePaths[fileIndex] + " (" + (fileIndex + 1) + "/" + filePaths.Length + ")");
            if (massSpecFileFormat == ".RAW")
            {
                try
                {
                    file = ThermoDynamicData.InitiateDynamicConnection(filePaths[fileIndex]);
                }
                catch (Exception)
                {
                    if (!silent)
                    {
                        Console.WriteLine("Problem opening raw file " + filePaths[fileIndex] + "\nPress any key to exit");
                        Console.ReadKey();
                    }
                    return file;
                }
            }
            else if (massSpecFileFormat == ".MZML")
            {
                try
                {
                    file = Mzml.LoadAllStaticData(filePaths[fileIndex]);
                }
                catch (Exception)
                {
                    if (!silent)
                    {
                        Console.WriteLine("Problem opening mzML file " + filePaths[fileIndex] + "\nPress any key to exit");
                        Console.ReadKey();
                    }
                    return file;
                }
            }
            else
            {
                if (!silent)
                {
                    Console.WriteLine("\nUnsupported file format\nPress any key to exit");
                    Console.ReadKey();
                }
                return file;
            }

            // input is good
            return file;
        }

        public void ConstructBinsFromIdentifications()
        {
            mzBinsTemplate = new Dictionary<double, List<FlashLFQMzBinElement>>();

            var peptideGroups = allIdentifications.GroupBy(p => p.FullSequence).ToList();
            var peptideBaseSeqs = new HashSet<string>(allIdentifications.Select(p => p.BaseSequence));
            var numCarbonsToIsotopicDistribution = new Dictionary<int, IsotopicDistribution>();
            baseSequenceToIsotopicDistribution = new Dictionary<string, List<KeyValuePair<double, double>>>();

            foreach (var baseSeq in peptideBaseSeqs)
            {
                if (baseSequenceToIsotopicDistribution.ContainsKey(baseSeq))
                    continue;

                Peptide p = new Peptide(baseSeq);
                int numCarbonsInThisPeptide = p.ElementCountWithIsotopes("C");

                // get expected C13 mass shifts and abundances
                IsotopicDistribution isotopicDistribution;
                if (!numCarbonsToIsotopicDistribution.TryGetValue(numCarbonsInThisPeptide, out isotopicDistribution))
                {
                    isotopicDistribution = IsotopicDistribution.GetDistribution(ChemicalFormula.ParseFormula("C" + numCarbonsInThisPeptide), 0.00001, 0.001);
                    numCarbonsToIsotopicDistribution.Add(numCarbonsInThisPeptide, isotopicDistribution);
                }

                var masses = isotopicDistribution.Masses.ToArray();
                var abundances = isotopicDistribution.Intensities.ToArray();
                var isotopicMassesAndNormalizedAbundances = new List<KeyValuePair<double, double>>();

                var monoisotopicMass = masses.Min();
                var highestAbundance = abundances.Max();

                for (int i = 0; i < masses.Length; i++)
                {
                    // expected isotopic mass shifts for peptide of this length
                    masses[i] -= monoisotopicMass;

                    // normalized abundance of each mass shift
                    abundances[i] /= highestAbundance;

                    // look for these isotopes
                    if (i < (numIsotopesRequired - 1) || abundances[i] > 0.1)
                        isotopicMassesAndNormalizedAbundances.Add(new KeyValuePair<double, double>(masses[i], abundances[i]));
                }

                baseSequenceToIsotopicDistribution.Add(baseSeq, isotopicMassesAndNormalizedAbundances);
            }

            var minChargeState = allIdentifications.Select(p => p.chargeState).Min();
            var maxChargeState = allIdentifications.Select(p => p.chargeState).Max();
            chargeStates = Enumerable.Range(minChargeState, (maxChargeState - minChargeState) + 1);

            // build theoretical m/z bins
            foreach (var pepGroup in peptideGroups)
            {
                double lowestCommonMassShift = baseSequenceToIsotopicDistribution[pepGroup.First().BaseSequence].Select(p => p.Key).Min();
                var mostCommonIsotopeShift = baseSequenceToIsotopicDistribution[pepGroup.First().BaseSequence].Where(p => p.Value == 1).First().Key;

                var thisPeptidesLowestCommonMass = pepGroup.First().monoisotopicMass + lowestCommonMassShift;
                var thisPeptidesMostAbundantMass = pepGroup.First().monoisotopicMass + mostCommonIsotopeShift;

                foreach (var pep in pepGroup)
                {
                    //pep.massToLookFor = thisPeptidesMostAbundantMass;
                    pep.massToLookFor = pepGroup.First().monoisotopicMass;
                    //pep.massToLookFor = thisPeptidesLowestCommonMass;
                }

                foreach (var chargeState in chargeStates)
                {
                    var t = ClassExtensions.ToMz(pepGroup.First().massToLookFor, chargeState);
                    double floorMz = Math.Floor(t * 100) / 100;
                    double ceilingMz = Math.Ceiling(t * 100) / 100;

                    if (!mzBinsTemplate.ContainsKey(floorMz))
                        mzBinsTemplate.Add(floorMz, new List<FlashLFQMzBinElement>());
                    if (!mzBinsTemplate.ContainsKey(ceilingMz))
                        mzBinsTemplate.Add(ceilingMz, new List<FlashLFQMzBinElement>());
                }
            }
        }

        private Dictionary<double, List<FlashLFQMzBinElement>> ConstructLocalBins()
        {
            return mzBinsTemplate.ToDictionary(v => v.Key, v => new List<FlashLFQMzBinElement>());
        }

        private List<KeyValuePair<int, double>> FillBinsWithPeaks(Dictionary<double, List<FlashLFQMzBinElement>> mzBins, IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file)
        {
            var allMs1Scans = new List<IMsDataScan<IMzSpectrum<IMzPeak>>>();
            var ms1ScanNumbersWithRetentionTimes = new List<KeyValuePair<int, double>>();

            // thermo files read differently than mzml
            var thermoFile = file as ThermoDynamicData;
            if (thermoFile != null)
            {
                int[] msOrders = thermoFile.ThermoGlobalParams.msOrderByScan;
                for (int i = 0; i < msOrders.Length; i++)
                    if (msOrders[i] == 1)
                        allMs1Scans.Add(thermoFile.GetOneBasedScan(i + 1));
            }
            else
                allMs1Scans = file.Where(s => s.MsnOrder == 1).ToList();

            foreach (var scan in allMs1Scans)
                ms1ScanNumbersWithRetentionTimes.Add(new KeyValuePair<int, double>(scan.OneBasedScanNumber, scan.RetentionTime));
            ms1ScanNumbersWithRetentionTimes = ms1ScanNumbersWithRetentionTimes.OrderBy(p => p.Key).ToList();

            if (!silent)
                Console.WriteLine("Assigning MS1 peaks to bins");
            
            //multithreaded bin-filling
            var allGoodPeaks = new List<List<KeyValuePair<double, FlashLFQMzBinElement>>>();

            Parallel.ForEach(Partitioner.Create(0, allMs1Scans.Count),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreesOfParallelism },
                (range, loopState) =>
                {
                    var threadLocalGoodPeaks = new List<KeyValuePair<double, FlashLFQMzBinElement>>();

                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        int peakIndexInThisScan = 0;

                        foreach (var peak in allMs1Scans[i].MassSpectrum)
                        {
                            FlashLFQMzBinElement element = null;
                            double floorMz = (Math.Floor(peak.Mz * 100) / 100);
                            double ceilingMz = (Math.Ceiling(peak.Mz * 100) / 100);

                            if (mzBins.ContainsKey(floorMz))
                            {
                                element = new FlashLFQMzBinElement(peak, allMs1Scans[i], peakIndexInThisScan);
                                threadLocalGoodPeaks.Add(new KeyValuePair<double, FlashLFQMzBinElement>(floorMz, element));
                            }
                            if (mzBins.ContainsKey(ceilingMz))
                            {
                                if (element == null)
                                    element = new FlashLFQMzBinElement(peak, allMs1Scans[i], peakIndexInThisScan);
                                threadLocalGoodPeaks.Add(new KeyValuePair<double, FlashLFQMzBinElement>(ceilingMz, element));
                            }

                            peakIndexInThisScan++;
                        }
                    }

                    lock (allGoodPeaks)
                        allGoodPeaks.Add(threadLocalGoodPeaks);
                }
            );

            Parallel.ForEach(Partitioner.Create(0, allGoodPeaks.Count), (range) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    foreach (var element in allGoodPeaks[i])
                    {
                        var t = mzBins[element.Key];
                        lock (t)
                            t.Add(element.Value);
                    }
                }
            });

            /*
            // single-threaded bin-filling
            foreach (var scan in allMs1Scans)
            {
                int peakIndexInThisScan = 0;

                foreach (var peak in scan.MassSpectrum)
                {
                    List<FlashLFQMzBinElement> mzBin;
                    FlashLFQMzBinElement element = null;
                    double floorMz = Math.Floor(peak.Mz * 100) / 100;
                    double ceilingMz = Math.Ceiling(peak.Mz * 100) / 100;

                    if (mzBins.TryGetValue(floorMz, out mzBin))
                    {
                        element = new FlashLFQMzBinElement(peak, scan, peakIndexInThisScan);
                        mzBin.Add(element);
                    }

                    if (mzBins.TryGetValue(ceilingMz, out mzBin))
                    {
                        if (element == null)
                            element = new FlashLFQMzBinElement(peak, scan, peakIndexInThisScan);
                        mzBin.Add(element);
                    }

                    peakIndexInThisScan++;
                }
            }
            */

            return ms1ScanNumbersWithRetentionTimes;
        }

        private List<FlashLFQFeature> MainFileSearch(string fileName, Dictionary<double, List<FlashLFQMzBinElement>> mzBins, List<KeyValuePair<int, double>> ms1ScanNumbersWithRts, IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file)
        {
            if (!silent)
                Console.WriteLine("Quantifying peptides for " + fileName);

            var ms1ScanNumbers = ms1ScanNumbersWithRts.Select(p => p.Key).OrderBy(p => p).ToList();
            var concurrentBagOfFeatures = new ConcurrentBag<FlashLFQFeature>();

            var groups = allIdentifications.GroupBy(p => p.fileName);
            var identificationsForThisFile = groups.Where(p => p.Key.Equals(fileName)).FirstOrDefault();

            if (identificationsForThisFile == null)
                return concurrentBagOfFeatures.ToList();

            var identifications = identificationsForThisFile.ToList();

            Parallel.ForEach(Partitioner.Create(0, identifications.Count),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreesOfParallelism },
                (range, loopState) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        var identification = identifications[i];
                        FlashLFQFeature msmsFeature = new FlashLFQFeature();
                        msmsFeature.identifyingScans.Add(identification);
                        msmsFeature.isMbrFeature = false;
                        msmsFeature.fileName = fileName;

                        foreach (var chargeState in chargeStates)
                        {
                            if (idSpecificChargeState)
                                if (chargeState != identification.chargeState)
                                    continue;

                            double theorMzHere = ClassExtensions.ToMz(identification.massToLookFor, chargeState);
                            double mzTolHere = (peakfindingPpmTolerance / 1e6) * theorMzHere;

                            double floorMz = Math.Floor(theorMzHere * 100) / 100;
                            double ceilingMz = Math.Ceiling(theorMzHere * 100) / 100;

                            IEnumerable<FlashLFQMzBinElement> binPeaks = new List<FlashLFQMzBinElement>();
                            List<FlashLFQMzBinElement> list;
                            if (mzBins.TryGetValue(floorMz, out list))
                                binPeaks = binPeaks.Concat(list);
                            if (mzBins.TryGetValue(ceilingMz, out list))
                                binPeaks = binPeaks.Concat(list);

                            // filter by mz tolerance
                            var binPeaksHere = binPeaks.Where(p => Math.Abs(p.mainPeak.Mz - theorMzHere) < mzTolHere);
                            // remove duplicates
                            binPeaksHere = binPeaksHere.Distinct();
                            // filter by RT
                            binPeaksHere = binPeaksHere.Where(p => Math.Abs(p.retentionTime - identification.ms2RetentionTime) < rtTol);

                            if (binPeaksHere.Any())
                            {
                                // get precursor scan to start at
                                int precursorScanNum = 0;
                                foreach (var ms1Scan in ms1ScanNumbersWithRts)
                                {
                                    if (ms1Scan.Value < identification.ms2RetentionTime)
                                        precursorScanNum = ms1Scan.Key;
                                    else
                                        break;
                                }
                                if (precursorScanNum == 0)
                                    throw new Exception("Error getting precursor scan number");

                                // separate peaks by rt into left and right of the identification RT
                                var rightPeaks = binPeaksHere.Where(p => p.retentionTime >= identification.ms2RetentionTime).OrderBy(p => p.retentionTime);
                                var leftPeaks = binPeaksHere.Where(p => p.retentionTime < identification.ms2RetentionTime).OrderByDescending(p => p.retentionTime);

                                // store peaks on each side of the identification RT
                                var crawledRightPeaks = ScanCrawl(rightPeaks, missedScansAllowed, precursorScanNum, ms1ScanNumbers);
                                var crawledLeftPeaks = ScanCrawl(leftPeaks, missedScansAllowed, precursorScanNum, ms1ScanNumbers);

                                // filter again by smaller mz tolerance
                                mzTolHere = (ppmTolerance / 1e6) * theorMzHere;
                                var validPeaks = crawledRightPeaks.Concat(crawledLeftPeaks);
                                validPeaks = validPeaks.Where(p => Math.Abs(p.mainPeak.Mz - theorMzHere) < mzTolHere);

                                // filter by isotopic distribution
                                var validIsotopeClusters = FilterPeaksByIsotopicDistribution(validPeaks, identification, chargeState, false);

                                // if multiple mass spectral peaks in the same scan are valid, pick the one with the smallest mass error
                                var peaksInSameScan = validIsotopeClusters.GroupBy(p => p.peakWithScan.oneBasedScanNumber).Where(v => v.Count() > 1);
                                if (peaksInSameScan.Any())
                                {
                                    foreach (var group in peaksInSameScan)
                                    {
                                        var mzToUse = group.Select(p => Math.Abs(p.peakWithScan.mainPeak.Mz - theorMzHere)).Min();
                                        var peakToUse = group.Where(p => Math.Abs(p.peakWithScan.mainPeak.Mz - theorMzHere) == mzToUse).First();
                                        var peaksToRemove = group.Where(p => p != peakToUse);
                                        validIsotopeClusters = validIsotopeClusters.Except(peaksToRemove);
                                    }
                                }

                                foreach (var validCluster in validIsotopeClusters)
                                    msmsFeature.isotopeClusters.Add(validCluster);
                            }
                        }

                        msmsFeature.CalculateIntensityForThisFeature(integrate);
                        CutPeak(msmsFeature, integrate);
                        concurrentBagOfFeatures.Add(msmsFeature);
                    }
                }
            );

            // merge results from all threads together
            return concurrentBagOfFeatures.ToList();
        }

        private void MatchBetweenRuns(string thisFileName, Dictionary<double, List<FlashLFQMzBinElement>> mzBins, List<FlashLFQFeature> features)
        {
            if (!silent)
                Console.WriteLine("Finding possible matched peptides for " + thisFileName);

            var concurrentBagOfMatchedFeatures = new ConcurrentBag<FlashLFQFeature>();
            var identificationsFromOtherRunsToLookFor = new List<FlashLFQIdentification>();
            var idsGroupedByFullSeq = allIdentifications.GroupBy(p => p.FullSequence);

            foreach (var fullSequenceGroup in idsGroupedByFullSeq)
            {
                // look for peptides with no ID's in this file
                var seqsByFilename = fullSequenceGroup.GroupBy(p => p.fileName);

                if (!seqsByFilename.Where(p => p.Key.Equals(thisFileName)).Any())
                    identificationsFromOtherRunsToLookFor.AddRange(fullSequenceGroup);
            }

            Parallel.ForEach(Partitioner.Create(0, identificationsFromOtherRunsToLookFor.Count),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreesOfParallelism },
                (range, loopState) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        var identification = identificationsFromOtherRunsToLookFor[i];

                        FlashLFQFeature mbrFeature = new FlashLFQFeature();
                        mbrFeature.identifyingScans.Add(identification);
                        mbrFeature.isMbrFeature = true;
                        mbrFeature.fileName = thisFileName;

                        foreach (var chargeState in chargeStates)
                        {
                            double theorMzHere = ClassExtensions.ToMz(identification.massToLookFor, chargeState);
                            double mzTolHere = (mbrppmTolerance / 1e6) * theorMzHere;

                            double floorMz = Math.Floor(theorMzHere * 100) / 100;
                            double ceilingMz = Math.Ceiling(theorMzHere * 100) / 100;

                            IEnumerable<FlashLFQMzBinElement> binPeaks = new List<FlashLFQMzBinElement>();
                            List<FlashLFQMzBinElement> t;
                            if (mzBins.TryGetValue(floorMz, out t))
                                binPeaks = binPeaks.Concat(t);
                            if (mzBins.TryGetValue(ceilingMz, out t))
                                binPeaks = binPeaks.Concat(t);

                            // filter by mz tolerance
                            var binPeaksHere = binPeaks.Where(p => Math.Abs(p.mainPeak.Mz - theorMzHere) < mzTolHere);
                            // filter by rt
                            binPeaksHere = binPeaksHere.Where(p => Math.Abs(p.retentionTime - identification.ms2RetentionTime) < mbrRtWindow);
                            // remove duplicates
                            binPeaksHere = binPeaksHere.Distinct();
                            // filter by isotopic distribution
                            var validIsotopeClusters = FilterPeaksByIsotopicDistribution(binPeaksHere, identification, chargeState, true);

                            if (validIsotopeClusters.Any())
                            {
                                double apexIntensity = validIsotopeClusters.Select(p => p.isotopeClusterIntensity).Max();
                                var mbrApexPeak = validIsotopeClusters.Where(p => p.isotopeClusterIntensity == apexIntensity).First();

                                mbrFeature.isotopeClusters.Add(mbrApexPeak);
                                //mbrFeature.isotopeClusters.AddRange(validIsotopeClusters);
                            }
                        }

                        if (mbrFeature.isotopeClusters.Any())
                        {
                            mbrFeature.CalculateIntensityForThisFeature(integrate);
                            concurrentBagOfMatchedFeatures.Add(mbrFeature);
                        }
                    }
                }
            );

            features.AddRange(concurrentBagOfMatchedFeatures);
        }

        private void RunErrorChecking(List<FlashLFQFeature> features)
        {
            if (!silent)
                Console.WriteLine("Checking errors");
            var featuresWithSamePeak = features.Where(v => v.intensity != 0).GroupBy(p => p.apexPeak.peakWithScan);
            featuresWithSamePeak = featuresWithSamePeak.Where(p => p.Count() > 1);

            // condense duplicate features
            foreach (var duplicateFeature in featuresWithSamePeak)
                duplicateFeature.First().MergeFeatureWith(duplicateFeature, integrate);
            features.RemoveAll(p => p.intensity == -1);

            // check for multiple features per peptide within a time window
            var featuresToMaybeMerge = features.Where(p => p.numIdentificationsByFullSeq == 1 && p.apexPeak != null).GroupBy(p => p.identifyingScans.First().FullSequence).Where(p => p.Count() > 1);
            if (featuresToMaybeMerge.Any())
            {
                foreach (var group in featuresToMaybeMerge)
                {
                    if (idSpecificChargeState)
                    {
                        var group2 = group.ToList().GroupBy(p => p.apexPeak.chargeState).Where(v => v.Count() > 1);

                        foreach (var group3 in group2)
                        {
                            foreach (var feature in group3)
                            {
                                if (feature.intensity != -1)
                                {
                                    var featuresToMerge = group.Where(p => Math.Abs(p.apexPeak.peakWithScan.retentionTime - feature.apexPeak.peakWithScan.retentionTime) < rtTol && p.intensity != -1);
                                    if (featuresToMerge.Any())
                                        feature.MergeFeatureWith(featuresToMerge, integrate);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var feature in group)
                        {
                            if (feature.intensity != -1)
                            {
                                var featuresToMerge = group.Where(p => Math.Abs(p.apexPeak.peakWithScan.retentionTime - feature.apexPeak.peakWithScan.retentionTime) < rtTol && p.intensity != -1);
                                if (featuresToMerge.Any())
                                    feature.MergeFeatureWith(featuresToMerge, integrate);
                            }
                        }
                    }
                }

                features.RemoveAll(p => p.intensity == -1);
            }

            if (errorCheckAmbiguousMatches)
            {
                // check for multiple peptides per feature
                var scansWithMultipleDifferentIds = features.Where(p => p.numIdentificationsByFullSeq > 1);
                var ambiguousFeatures = scansWithMultipleDifferentIds.Where(p => p.numIdentificationsByBaseSeq > 1);

                // handle ambiguous features
                foreach (var ambiguousFeature in ambiguousFeatures)
                {
                    var msmsIdentsForThisFile = ambiguousFeature.identifyingScans.Where(p => p.fileName.Equals(ambiguousFeature.fileName));

                    if (!msmsIdentsForThisFile.Any())
                    {
                        ambiguousFeature.intensity = -1;
                    }
                    else
                    {
                        ambiguousFeature.identifyingScans = msmsIdentsForThisFile.ToList();
                    }
                }

                features.RemoveAll(p => p.intensity == -1);
            }
        }

        public IOrderedEnumerable<FlashLFQSummedFeatureGroup> SumFeatures(IEnumerable<FlashLFQFeature> features)
        {
            List<FlashLFQSummedFeatureGroup> returnList = new List<FlashLFQSummedFeatureGroup>();

            string[] fileNames = new string[filePaths.Length];
            for (int i = 0; i < fileNames.Length; i++)
                fileNames[i] = Path.GetFileNameWithoutExtension(filePaths[i]);
            FlashLFQSummedFeatureGroup.files = fileNames;

            var baseSeqToFeatureMatch = new Dictionary<string, List<FlashLFQFeature>>();
            foreach (var feature in features)
            {
                var baseSeqs = feature.identifyingScans.GroupBy(p => p.BaseSequence);

                foreach (var seq in baseSeqs)
                {
                    List<FlashLFQFeature> featuresForThisBaseSeq;
                    if (baseSeqToFeatureMatch.TryGetValue(seq.Key, out featuresForThisBaseSeq))
                        featuresForThisBaseSeq.Add(feature);
                    else
                        baseSeqToFeatureMatch.Add(seq.Key, new List<FlashLFQFeature>() { feature });
                }
            }

            foreach (var sequence in baseSeqToFeatureMatch)
            {
                double[] intensitiesByFile = new double[filePaths.Length];
                string[] identificationType = new string[filePaths.Length];
                var thisSeqPerFile = sequence.Value.GroupBy(p => p.fileName);

                for (int i = 0; i < intensitiesByFile.Length; i++)
                {
                    string file = Path.GetFileNameWithoutExtension(filePaths[i]);
                    var featuresForThisBaseSeqAndFile = thisSeqPerFile.Where(p => p.Key.Equals(file)).FirstOrDefault();

                    if (featuresForThisBaseSeqAndFile != null)
                    {
                        if (featuresForThisBaseSeqAndFile.First().isMbrFeature)
                        {
                            identificationType[i] = "MBR";
                            intensitiesByFile[i] = featuresForThisBaseSeqAndFile.Select(p => p.intensity).Max();
                        }
                        else
                        {
                            identificationType[i] = "MSMS";
                            double summedPeakIntensity = featuresForThisBaseSeqAndFile.Sum(p => p.intensity);

                            if (featuresForThisBaseSeqAndFile.Max(p => p.numIdentificationsByBaseSeq) == 1)
                                intensitiesByFile[i] = summedPeakIntensity;
                            else
                            {
                                double ambigPeakIntensity = featuresForThisBaseSeqAndFile.Where(p => p.numIdentificationsByBaseSeq > 1).Sum(v => v.intensity);

                                if ((ambigPeakIntensity / summedPeakIntensity) < 0.3)
                                    intensitiesByFile[i] = featuresForThisBaseSeqAndFile.Select(p => (p.intensity / p.numIdentificationsByBaseSeq)).Sum();
                                else
                                    intensitiesByFile[i] = 0;
                            }
                        }
                        //if (featuresForThisBaseSeqAndFile.Where(p => p.couldBeBadPeak == true).Any())
                        //    intensitiesByFile[i] = 0;
                    }
                    else
                        identificationType[i] = "";
                }
                
                returnList.Add(new FlashLFQSummedFeatureGroup(sequence.Key + "\t" + sequence.Value.First().identifyingScans.First().proteinGroup.proteinGroupName, intensitiesByFile, identificationType));
            }

            return returnList.OrderBy(p => p.BaseSequence);
        }

        private IEnumerable<FlashLFQIsotopeCluster> FilterPeaksByIsotopicDistribution(IEnumerable<FlashLFQMzBinElement> peaks, FlashLFQIdentification identification, int chargeState, bool lookForBadIsotope)
        {
            var isotopeClusters = new List<FlashLFQIsotopeCluster>();
            var isotopeMassShifts = baseSequenceToIsotopicDistribution[identification.BaseSequence];

            if (isotopeMassShifts.Count < numIsotopesRequired)
                return isotopeClusters;

            foreach (var thisPeakWithScan in peaks)
            {
                // calculate theoretical isotopes
                var theorIsotopeMzs = new double[isotopeMassShifts.Count];
                var mainpeakMz = thisPeakWithScan.mainPeak.Mz;
                for (int i = 0; i < isotopeMassShifts.Count; i++)
                    theorIsotopeMzs[i] = mainpeakMz + (isotopeMassShifts[i].Key / chargeState);
                theorIsotopeMzs = theorIsotopeMzs.OrderBy(p => p).ToArray();

                var lowestMzIsotopePossible = theorIsotopeMzs.First();
                lowestMzIsotopePossible -= (ppmTolerance / 1e6) * lowestMzIsotopePossible;
                var highestMzIsotopePossible = theorIsotopeMzs.Last();
                highestMzIsotopePossible += (ppmTolerance / 1e6) * highestMzIsotopePossible;

                // get possible isotope peaks from the peak's scan
                List<IMzPeak> possibleIsotopePeaks = new List<IMzPeak>();

                for (int i = thisPeakWithScan.zeroBasedIndexOfPeakInScan; i < thisPeakWithScan.scan.MassSpectrum.Size; i++)
                {
                    if (thisPeakWithScan.scan.MassSpectrum[i].Mz > highestMzIsotopePossible)
                        break;
                    possibleIsotopePeaks.Add(thisPeakWithScan.scan.MassSpectrum[i]);
                }

                int isotopeIndex = 0;
                double theorIsotopeMz = theorIsotopeMzs[0];
                double isotopeMzTol = (isotopePpmTolerance / 1e6) * theorIsotopeMzs[0];

                if (lookForBadIsotope)
                {
                    bool badPeak = false;
                    double tol = (isotopePpmTolerance / 1e6) * theorIsotopeMz;
                    double prevIsotopePeakMz = (mainpeakMz - (1.003322 / chargeState));

                    for (int i = thisPeakWithScan.zeroBasedIndexOfPeakInScan; i > 0; i--)
                    {
                        var peak = thisPeakWithScan.scan.MassSpectrum[i];
                        if (Math.Abs(peak.Mz - prevIsotopePeakMz) < tol)
                            if (peak.Intensity / thisPeakWithScan.mainPeak.Intensity > 0.2)
                                badPeak = true;
                        if (peak.Mz < (prevIsotopePeakMz - tol))
                            break;
                    }

                    if (badPeak)
                        continue;
                }

                // isotopic distribution check
                bool isotopeDistributionCheck = false;
                IMzPeak[] isotopePeaks = new IMzPeak[isotopeMassShifts.Count];
                foreach (var possibleIsotopePeak in possibleIsotopePeaks)
                {
                    if (Math.Abs(possibleIsotopePeak.Mz - theorIsotopeMz) < isotopeMzTol)
                    {
                        // store the good isotope peak
                        isotopePeaks[isotopeIndex] = possibleIsotopePeak;

                        if (isotopeIndex < isotopePeaks.Length - 1)
                        {
                            // look for the next isotope
                            isotopeIndex++;

                            theorIsotopeMz = theorIsotopeMzs[isotopeIndex];
                            isotopeMzTol = (isotopePpmTolerance / 1e6) * theorIsotopeMzs[isotopeIndex];
                        }
                    }
                }

                // all isotopes have been looked for - check to see if they've been observed
                bool[] requiredIsotopeSeen = new bool[numIsotopesRequired];

                for (int i = 0; i < numIsotopesRequired; i++)
                {
                    if (isotopePeaks[i] != null)
                        requiredIsotopeSeen[i] = true;
                    else
                        requiredIsotopeSeen[i] = false;
                }

                if (requiredIsotopeSeen.Where(p => p.Equals(true)).Count() == numIsotopesRequired)
                    isotopeDistributionCheck = true;

                if (isotopeDistributionCheck)
                {
                    double isotopeClusterIntensity = 0;

                    for (int i = 0; i < isotopePeaks.Length; i++)
                    {
                        if (isotopePeaks[i] != null)
                        {
                            double relIsotopeAbundance = isotopePeaks[i].Intensity / isotopePeaks[0].Intensity;
                            double theorIsotopeAbundance = isotopeMassShifts[i].Value / isotopeMassShifts[0].Value;

                            // impute isotope intensity if it is very different from expected
                            if ((relIsotopeAbundance / theorIsotopeAbundance) < 3.0)
                                isotopeClusterIntensity += isotopePeaks[i].Intensity;
                            else
                                isotopeClusterIntensity += theorIsotopeAbundance * isotopePeaks[0].Intensity;
                        }
                        else
                            isotopeClusterIntensity += (isotopeMassShifts[i].Value / isotopeMassShifts[0].Value) * isotopePeaks[0].Intensity;
                    }

                    isotopeClusters.Add(new FlashLFQIsotopeCluster(thisPeakWithScan, chargeState, isotopeClusterIntensity));
                }
            }

            return isotopeClusters;
        }

        private IEnumerable<FlashLFQMzBinElement> ScanCrawl(IOrderedEnumerable<FlashLFQMzBinElement> peaksWithScans, int missedScansAllowed, int startingMS1ScanNumber, List<int> ms1ScanNumbers)
        {
            var validPeaksWithScans = new List<FlashLFQMzBinElement>();

            int lastGoodIndex = ms1ScanNumbers.IndexOf(startingMS1ScanNumber);
            int ms1IndexHere = lastGoodIndex - 1;
            int missedScans = 0;

            foreach (var thisPeakWithScan in peaksWithScans)
            {
                ms1IndexHere = ms1ScanNumbers.IndexOf(thisPeakWithScan.oneBasedScanNumber);
                missedScans += Math.Abs(ms1IndexHere - lastGoodIndex) - 1;

                if (missedScans > missedScansAllowed)
                    break;

                // found a good peak; reset missed scans to 0
                missedScans = 0;
                lastGoodIndex = ms1IndexHere;

                validPeaksWithScans.Add(thisPeakWithScan);
            }

            return validPeaksWithScans;
        }

        private void CutPeak(FlashLFQFeature peak, bool integrate)
        {
            bool cutThisPeak = false;
            FlashLFQIsotopeCluster valleyTimePoint = null;

            if (peak.isotopeClusters.Count() < 5)
                return;

            // find out if we need to split this peak by using the discrimination factor
            var timePointsForApexZ = peak.isotopeClusters.Where(p => p.chargeState == peak.apexPeak.chargeState);
            var leftTimePoints = timePointsForApexZ.Where(p => p.peakWithScan.retentionTime <= peak.apexPeak.peakWithScan.retentionTime).OrderByDescending(v => v.peakWithScan.retentionTime);
            var rightTimePoints = timePointsForApexZ.Where(p => p.peakWithScan.retentionTime >= peak.apexPeak.peakWithScan.retentionTime).OrderBy(v => v.peakWithScan.retentionTime);

            double mind0 = 0.6;

            foreach (var timePoint in rightTimePoints)
            {
                if (valleyTimePoint == null || timePoint.isotopeClusterIntensity < valleyTimePoint.isotopeClusterIntensity)
                    valleyTimePoint = timePoint;

                var timePointsBetweenApexAndThisTimePoint = rightTimePoints.Where(p => p.peakWithScan.retentionTime <= timePoint.peakWithScan.retentionTime).ToList();

                var d0 = (timePoint.isotopeClusterIntensity - valleyTimePoint.isotopeClusterIntensity) / timePoint.isotopeClusterIntensity;
                if (d0 > mind0)
                {
                    var secondValleyTimePoint = timePointsBetweenApexAndThisTimePoint[timePointsBetweenApexAndThisTimePoint.IndexOf(valleyTimePoint) + 1];

                    d0 = (timePoint.isotopeClusterIntensity - secondValleyTimePoint.isotopeClusterIntensity) / timePoint.isotopeClusterIntensity;

                    if (d0 > mind0)
                    {
                        cutThisPeak = true;
                        break;
                    }
                }
            }

            if (cutThisPeak == false)
            {
                valleyTimePoint = null;

                foreach (var timePoint in leftTimePoints)
                {
                    if (valleyTimePoint == null || timePoint.isotopeClusterIntensity < valleyTimePoint.isotopeClusterIntensity)
                        valleyTimePoint = timePoint;

                    var timePointsBetweenApexAndThisTimePoint = leftTimePoints.Where(p => p.peakWithScan.retentionTime >= timePoint.peakWithScan.retentionTime).ToList();

                    var d0 = (timePoint.isotopeClusterIntensity - valleyTimePoint.isotopeClusterIntensity) / timePoint.isotopeClusterIntensity;
                    if (d0 > mind0)
                    {
                        var secondValleyTimePoint = timePointsBetweenApexAndThisTimePoint[timePointsBetweenApexAndThisTimePoint.IndexOf(valleyTimePoint) + 1];

                        d0 = (timePoint.isotopeClusterIntensity - secondValleyTimePoint.isotopeClusterIntensity) / timePoint.isotopeClusterIntensity;

                        if (d0 > mind0)
                        {
                            cutThisPeak = true;
                            break;
                        }
                    }
                }
            }

            // cut
            if (cutThisPeak)
            {
                var splitLeft = peak.isotopeClusters.Where(p => p.peakWithScan.retentionTime <= valleyTimePoint.peakWithScan.retentionTime).ToList();
                var splitRight = peak.isotopeClusters.Where(p => p.peakWithScan.retentionTime >= valleyTimePoint.peakWithScan.retentionTime).ToList();

                if (peak.identifyingScans.First().ms2RetentionTime > splitLeft.Max(p => p.peakWithScan.retentionTime))
                    foreach (var timePoint in splitLeft)
                        peak.isotopeClusters.Remove(timePoint);
                else
                    foreach (var timePoint in splitRight)
                        peak.isotopeClusters.Remove(timePoint);

                // recalculate intensity for the peak
                peak.CalculateIntensityForThisFeature(integrate);
                peak.splitRT = valleyTimePoint.peakWithScan.retentionTime;

                // recursively cut
                CutPeak(peak, integrate);
            }
        }
    }
}