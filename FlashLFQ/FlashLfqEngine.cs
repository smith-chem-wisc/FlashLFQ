using Chemistry;
using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using Proteomics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UsefulProteomicsDatabases;

namespace FlashLFQ
{
    public class FlashLFQEngine
    {
        // file info stuff
        public string identificationsFilePath { get; private set; }
        public string[] filePaths { get; private set; }
        public string outputFolder;

        // structures used in the FlashLFQ program
        private List<FlashLFQIdentification> allIdentifications;
        public List<FlashLFQFeature>[] allFeaturesByFile { get; private set; }
        private Dictionary<double, List<FlashLFQMzBinElement>> mzBinsTemplate;
        private Dictionary<string, List<KeyValuePair<double, double>>> baseSequenceToIsotopicDistribution;
        private Dictionary<string, FlashLFQProteinGroup> pepToProteinGroupDictionary;
        private string[] header;

        // settings
        public bool silent { get; private set; }
        public bool pause { get; private set; }
        public int maxParallelFiles { get; private set; }
        public int maxDegreesOfParallelism { get; private set; }
        public IEnumerable<int> chargeStates { get; private set; }
        public double ppmTolerance { get; private set; }
        public double rtTol { get; private set; }
        public double isotopePpmTolerance { get; private set; }
        public bool integrate { get; private set; }
        public bool sumFeatures { get; private set; }
        public double initialRTWindow { get; private set; }
        public int missedScansAllowed { get; private set; }
        public int numIsotopesRequired { get; private set; }
        public double signalToBackgroundRequired { get; private set; }
        public double mbrRtWindow { get; private set; }
        public double mbrSbrFilter { get; private set; }
        public double mbrppmTolerance { get; private set; }
        public bool errorCheckAmbiguousMatches { get; private set; }
        public bool mbr { get; private set; }
        public double sbrFilter { get; private set; }
        public bool idSpecificChargeState { get; private set; }

        public FlashLFQEngine()
        {
            allIdentifications = new List<FlashLFQIdentification>();
            pepToProteinGroupDictionary = new Dictionary<string, FlashLFQProteinGroup>();

            // default parameters
            signalToBackgroundRequired = 5.0;
            sbrFilter = 5.0;
            mbrSbrFilter = 5.0;
            mbrRtWindow = 500.0;
            chargeStates = new List<int>();
            ppmTolerance = 10.0;
            mbrppmTolerance = 5.0;
            isotopePpmTolerance = 5.0;
            integrate = false;
            sumFeatures = true;
            initialRTWindow = 0.4;
            missedScansAllowed = 1;
            numIsotopesRequired = 2;
            rtTol = 5.0;
            silent = false;
            pause = true;
            errorCheckAmbiguousMatches = true;
            mbr = false;
            maxParallelFiles = 1;
            maxDegreesOfParallelism = -1;
            idSpecificChargeState = false;
        }

        public bool ParseArgs(string[] args)
        {
            string[] validArgs = new string[] { "--idt [string|identification file path (TSV format)]",
                "--raw [string|MS data file (.raw or .mzml)]", "--rep [string|directory containing MS data files]", "--ppm [double|ppm tolerance]",
                "--iso [double|isotopic distribution tolerance in ppm]", "--sil [bool|silent mode]",
                "--pau [bool|pause at end of run]", "--int [bool|integrate features]", "--sum [bool|sum features in a run]" };
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
                        case ("sum"): sumFeatures = Boolean.Parse(arg.Substring(3)); break;
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
            
            allFeaturesByFile = new List<FlashLFQFeature>[filePaths.Length];
            return true;
        }

        public void PassFilePaths(string[] paths)
        {
            filePaths = paths.Distinct().ToArray();
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

            // read identification file
            if (!silent)
                Console.WriteLine("Opening " + identificationsFilePath);
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
                        && header.Contains("Protein Accession"))
                    {
                        fileNameCol = Array.IndexOf(header, "File Name");
                        baseSequCol = Array.IndexOf(header, "Base Sequence");
                        fullSequCol = Array.IndexOf(header, "Full Sequence");
                        monoMassCol = Array.IndexOf(header, "Peptide Monoisotopic Mass");
                        msmsRetnCol = Array.IndexOf(header, "Scan Retention Time");
                        chargeStCol = Array.IndexOf(header, "Precursor Charge");
                        protNameCol = Array.IndexOf(header, "Protein Accession");
                    }

                    // Morpheus MS/MS input
                    else if (header.Contains("Filename")
                        && header.Contains("Base Peptide Sequence")
                        && header.Contains("Peptide Sequence")
                        && header.Contains("Theoretical Mass (Da)")
                        && header.Contains("Retention Time (minutes)")
                        && header.Contains("Precursor Charge")
                        && header.Contains("Protein Description"))
                    {
                        fileNameCol = Array.IndexOf(header, "Filename");
                        baseSequCol = Array.IndexOf(header, "Base Peptide Sequence");
                        fullSequCol = Array.IndexOf(header, "Peptide Sequence");
                        monoMassCol = Array.IndexOf(header, "Theoretical Mass (Da)");
                        msmsRetnCol = Array.IndexOf(header, "Retention Time (minutes)");
                        chargeStCol = Array.IndexOf(header, "Precursor Charge");
                        protNameCol = Array.IndexOf(header, "Protein Description");
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
                    }

                    // other search engines

                    // can't parse file
                    if (fileNameCol == -1)
                    {
                        if (!silent)
                        {
                            Console.WriteLine("File is improperly formatted");
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
                        double monoisotopicMass = double.Parse(param[monoMassCol]);
                        double ms2RetentionTime = double.Parse(param[msmsRetnCol]);
                        int chargeState = int.Parse(param[chargeStCol]);

                        var ident = new FlashLFQIdentification(fileName, BaseSequence, ModSequence, monoisotopicMass, ms2RetentionTime, chargeState);
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

            return true;
        }

        public void Quantify(IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file, string filePath)
        {
            if (filePaths == null)
                return;
            
            // construct bins
            var localBins = ConstructLocalBins();

            // open raw file
            int i = Array.IndexOf(filePaths, filePath);
            if (i < 0)
                return;
            var currentDataFile = file;
            if (currentDataFile == null)
                currentDataFile = ReadMSFile(i);
            if(currentDataFile == null)
                return;
            
            // fill bins with peaks from the raw file
            var ms1ScanNumbers = FillBinsWithPeaks(localBins, currentDataFile);

            // quantify features using this file's IDs first
            allFeaturesByFile[i] = MainFileSearch(Path.GetFileNameWithoutExtension(filePath), localBins, ms1ScanNumbers);

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
        }

        public bool WriteResults(string baseFileName, bool writePeaks, bool writePeptides, bool writeProteins)
        {
            try
            {
                if(outputFolder == null)
                    outputFolder = identificationsFilePath.Substring(0, identificationsFilePath.Length - (identificationsFilePath.Length - identificationsFilePath.IndexOf('.')));

                var allFeatures = allFeaturesByFile.SelectMany(p => p.Select(v => v));
                
                // write features
                header = new string[] { "Peaks Header" };
                List<string> featureOutput = new List<string> { string.Join("\t", header) };
                featureOutput = featureOutput.Concat(allFeatures.Select(v => v.ToString())).ToList();
                if (writePeaks)
                    File.WriteAllLines(outputFolder + baseFileName + "QuantifiedPeaks.tsv", featureOutput);

                // write baseseq groups
                var baseSeqOutputHeader = new string[1 + filePaths.Length];
                baseSeqOutputHeader[0] = "Peptide Header";
                for (int i = 1; i < baseSeqOutputHeader.Length; i++)
                    baseSeqOutputHeader[i] = Path.GetFileName(filePaths[i - 1]);
                List<string> baseSeqOutput = new List<string> { string.Join("\t", baseSeqOutputHeader) };
                baseSeqOutput = baseSeqOutput.Concat(SumFeatures(allFeatures).Select(p => p.ToString())).ToList();
                if(writePeptides)
                    File.WriteAllLines(outputFolder + baseFileName + "QuantifiedPeptides.tsv", baseSeqOutput);

                // write protein results
                var proteinGroups = allFeatures.Select(p => p.identifyingScans.First().proteinGroup).Where(v => v.intensitiesByFile != null).Distinct().OrderBy(p => p.proteinGroupName);
                List<string> proteinOutput = new List<string> { string.Join("\t", new string[] { "test" }) };
                proteinOutput = proteinOutput.Concat(proteinGroups.Select(v => v.ToString())).ToList();
                if(writeProteins)
                    File.WriteAllLines(outputFolder + baseFileName + "QuantifiedProteins.tsv", proteinOutput);
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
            // get features from both files, find RT difference
            //allFeaturesByFile
        }

        public void QuantifyProteins()
        {
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

            foreach(var proteinFeatures in featuresGroupedByProtein)
            {
                var featuresByFile = proteinFeatures.GroupBy(p => p.fileName);
                var pepBaseSeqs = proteinFeatures.Select(p => p.identifyingScans.First().BaseSequence).Distinct().ToList();

                // construct empty peptide/file array for this protein
                List<FlashLFQFeature>[,] temp = new List<FlashLFQFeature>[filePaths.Length, pepBaseSeqs.Count];
                for(int i = 0; i < temp.GetLength(0); i++)
                    for(int j = 0; j < temp.GetLength(1); j++)
                        temp[i,j] = new List<FlashLFQFeature>();
                
                // populate array
                foreach(var file in featuresByFile)
                {
                    int fileIndex = fileNames.IndexOf(file.Key);
                    foreach (var feature in file)
                        temp[fileIndex, pepBaseSeqs.IndexOf(feature.identifyingScans.First().BaseSequence)].Add(feature);
                }

                proteinFeatures.Key.intensitiesByFile = new double[fileNames.Count];
                for(int i = 0; i < fileNames.Count; i++)
                {
                    for (int j = 0; j < pepBaseSeqs.Count; j++)
                        proteinFeatures.Key.intensitiesByFile[i] += temp[i,j].Select(p => (p.intensity / p.numIdentificationsByFullSeq)).Sum();
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

        private IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> ReadMSFile(int fileIndex)
        {
            var massSpecFileFormat = filePaths[fileIndex].Substring(filePaths[fileIndex].IndexOf('.')).ToUpper();
            IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file = null;

            // read mass spec file
            if (!silent)
                Console.WriteLine("Opening " + filePaths[fileIndex]);
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
            chargeStates = Enumerable.Range(minChargeState, maxChargeState - 1);

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

        private List<int> FillBinsWithPeaks(Dictionary<double, List<FlashLFQMzBinElement>> mzBins, IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> file)
        {
            var allMs1Scans = new List<IMsDataScan<IMzSpectrum<IMzPeak>>>();

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
            
            var ms1ScanNumbers = allMs1Scans.Select(p => p.OneBasedScanNumber).Distinct().OrderBy(p => p).ToList();

            if (!silent)
                Console.WriteLine("Assigning MS1 peaks to bins");




            //int threads = Environment.ProcessorCount;
            //Dictionary<double, List<FlashLFQMzBinElement>>[] multithreadedDictionary = new Dictionary<double, List<FlashLFQMzBinElement>>[maxDegreesOfParallelism];


            /*
            Parallel.ForEach(Partitioner.Create(0, allMs1Scans.Count),
                // initialize thread-local mz bins
                () => mzBins.ToDictionary(x => x.Key, x => new List<FlashLFQMzBinElement>()),
                // process each ms1 scan block
                (scan, loopState, threadLocalDictionary) =>
                {
                    for (int i = scan.Item1; i < scan.Item2; i++)
                    {
                        int peakIndexInThisScan = 0;
                        double backgroundForThisScan = allMs1Scans[i].MassSpectrum.Select(p => p.Intensity).Min();

                        foreach (var peak in allMs1Scans[i].MassSpectrum)
                        {
                            double signalToBackgroundRatio = peak.Intensity / backgroundForThisScan;

                            if (signalToBackgroundRatio > signalToBackgroundRequired)
                            {
                                List<FlashLFQMzBinElement> mzBin;
                                FlashLFQMzBinElement element = new FlashLFQMzBinElement(peak, allMs1Scans[i], backgroundForThisScan, peakIndexInThisScan);
                                double floorMz = Math.Floor(peak.Mz * 100) / 100;
                                double ceilingMz = Math.Ceiling(peak.Mz * 100) / 100;

                                if (threadLocalDictionary.TryGetValue(floorMz, out mzBin))
                                {
                                    element = new FlashLFQMzBinElement(peak, allMs1Scans[i], backgroundForThisScan, peakIndexInThisScan);
                                    mzBin.Add(element);
                                }

                                if (threadLocalDictionary.TryGetValue(ceilingMz, out mzBin))
                                {
                                    if (element == null)
                                        element = new FlashLFQMzBinElement(peak, allMs1Scans[i], backgroundForThisScan, peakIndexInThisScan);
                                    mzBin.Add(element);
                                }
                            }

                            peakIndexInThisScan++;
                        }
                    }

                    return threadLocalDictionary;
                },
                // aggregate results from the individual threads
                threadLocalDictionary =>
                {
                    lock (mzBins)
                        foreach (var bin in mzBins.Keys)
                            mzBins[bin].AddRange(threadLocalDictionary[bin]);
                }
            );
            */

            
            foreach (var scan in allMs1Scans)
            {
                int peakIndexInThisScan = 0;
                double backgroundForThisScan = scan.MassSpectrum.Select(p => p.Intensity).Min();
                //double backgroundForThisScan = 1;

                foreach (var peak in scan.MassSpectrum)
                {
                    double signalToBackgroundRatio = peak.Intensity / backgroundForThisScan;

                    if (signalToBackgroundRatio > signalToBackgroundRequired)
                    {
                        List<FlashLFQMzBinElement> mzBin;
                        FlashLFQMzBinElement element = null;
                        double floorMz = Math.Floor(peak.Mz * 100) / 100;
                        double ceilingMz = Math.Ceiling(peak.Mz * 100) / 100;

                        if (mzBins.TryGetValue(floorMz, out mzBin))
                        {
                            element = new FlashLFQMzBinElement(peak, scan, backgroundForThisScan, peakIndexInThisScan);
                            mzBin.Add(element);
                        }

                        if (mzBins.TryGetValue(ceilingMz, out mzBin))
                        {
                            if (element == null)
                                element = new FlashLFQMzBinElement(peak, scan, backgroundForThisScan, peakIndexInThisScan);
                            mzBin.Add(element);
                        }
                    }

                    peakIndexInThisScan++;
                }
            }
            

            return ms1ScanNumbers;
        }

        private List<FlashLFQFeature> MainFileSearch(string fileName, Dictionary<double, List<FlashLFQMzBinElement>> mzBins, List<int> ms1ScanNumbers)
        {
            var groups = allIdentifications.GroupBy(p => p.fileName);
            var identificationsForThisFile = groups.Where(p => p.Key.Equals(fileName)).FirstOrDefault();
            if (identificationsForThisFile == null)
                return null;

            var identifications = identificationsForThisFile.ToList();
            var concurrentBagOfFeatures = new ConcurrentBag<FlashLFQFeature>();
            
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
                
                        foreach (var chargeState in chargeStates)
                        {
                            if (idSpecificChargeState)
                                if (chargeState != identification.chargeState)
                                    continue;

                            double theorMzHere = ClassExtensions.ToMz(identification.massToLookFor, chargeState);
                            double mzTolHere = (ppmTolerance / 1e6) * theorMzHere;

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

                            if (binPeaksHere.Any())
                            {
                                var prevPeaks = binPeaksHere.Where(p => p.retentionTime - identification.ms2RetentionTime < 0).Select(p => p.retentionTime - identification.ms2RetentionTime);
                                double bestRTDifference;

                                if (prevPeaks.Any())
                                {
                                    bestRTDifference = Math.Abs(prevPeaks.Max());

                                    if(bestRTDifference > initialRTWindow)
                                        bestRTDifference = bestRTDifference = binPeaksHere.Select(p => Math.Abs(p.retentionTime - identification.ms2RetentionTime)).Min();
                                }
                                else
                                    bestRTDifference = bestRTDifference = binPeaksHere.Select(p => Math.Abs(p.retentionTime - identification.ms2RetentionTime)).Min();
                                
                                if (bestRTDifference < initialRTWindow)
                                {
                                    // get first good peak nearest to the identification's RT to look around
                                    var bestPeakToStartAt = binPeaksHere.Where(p => bestRTDifference == Math.Abs(p.retentionTime - identification.ms2RetentionTime)).First();

                                    // filter by RT
                                    binPeaksHere = binPeaksHere.Where(p => Math.Abs(p.retentionTime - identification.ms2RetentionTime) < rtTol);

                                    // separate peaks by rt into left and right of the identification RT
                                    var rightPeaks = binPeaksHere.Where(p => p.retentionTime >= bestPeakToStartAt.retentionTime).OrderBy(p => p.retentionTime);
                                    var leftPeaks = binPeaksHere.Where(p => p.retentionTime < bestPeakToStartAt.retentionTime).OrderByDescending(p => p.retentionTime);

                                    // store peaks on each side of the identification RT
                                    var crawledRightPeaks = ScanCrawl(rightPeaks, missedScansAllowed, bestPeakToStartAt.oneBasedScanNumber, ms1ScanNumbers);
                                    var crawledLeftPeaks = ScanCrawl(leftPeaks, missedScansAllowed, bestPeakToStartAt.oneBasedScanNumber, ms1ScanNumbers);

                                    var validPeaks = crawledRightPeaks.Concat(crawledLeftPeaks);

                                    var validIsotopeClusters = FilterPeaksByIsotopicDistribution(validPeaks, identification, chargeState);
                                    
                                    foreach (var validCluster in validIsotopeClusters)
                                        msmsFeature.isotopeClusters.Add(validCluster);
                                }
                            }
                        }

                        msmsFeature.CalculateIntensityForThisFeature(fileName, integrate);
                        concurrentBagOfFeatures.Add(msmsFeature);
                    }
                }
            );
            
            // merge results from all threads together
            return concurrentBagOfFeatures.ToList();
        }

        private void MatchBetweenRuns(string thisFileName, Dictionary<double, List<FlashLFQMzBinElement>> mzBins, List<FlashLFQFeature> features)
        {
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
                            var validIsotopeClusters = FilterPeaksByIsotopicDistribution(binPeaksHere, identification, chargeState);

                            if (validIsotopeClusters.Any())
                            {
                                //double apexIntensity = validIsotopeClusters.Select(p => p.isotopeClusterIntensity).Max();
                                //var mbrApexPeak = validIsotopeClusters.Where(p => p.isotopeClusterIntensity == apexIntensity).First();
                                
                                //mbrFeature.isotopeClusters.Add(mbrApexPeak);
                                mbrFeature.isotopeClusters.AddRange(validIsotopeClusters);
                            }
                        }

                        if (mbrFeature.isotopeClusters.Any())
                        {
                            //mbrFeature.CalculateIntensityForThisFeature(thisFileName, integrate);
                            concurrentBagOfMatchedFeatures.Add(mbrFeature);
                        }
                    }
                }
            );

            features.AddRange(concurrentBagOfMatchedFeatures);
        }

        private void RunErrorChecking(List<FlashLFQFeature> features)
        {
            var featuresWithSamePeak = features.Where(v => v.intensity != 0).GroupBy(p => p.apexPeak.peakWithScan);
            featuresWithSamePeak = featuresWithSamePeak.Where(p => p.Count() > 1);

            // condense duplicate features
            foreach (var duplicateFeature in featuresWithSamePeak)
                duplicateFeature.First().MergeFeatureWith(duplicateFeature);
            // check for multiple features per peptide within a time window

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
                        //ambiguousFeature.intensity = 0;
                    }
                }

                foreach (var feature in features)
                {
                    if (feature.apexPeak != null && feature.apexPeak.peakWithScan.signalToBackgroundRatio < sbrFilter)
                        feature.intensity = 0;
                    if (feature.isMbrFeature && feature.apexPeak.peakWithScan.signalToBackgroundRatio < mbrSbrFilter)
                        feature.intensity = -1;
                }

                features.RemoveAll(p => p.intensity == -1);
            }
        }

        public IOrderedEnumerable<FlashLFQSummedFeatureGroup> SumFeatures(IEnumerable<FlashLFQFeature> features)
        {
            List<FlashLFQSummedFeatureGroup> returnList = new List<FlashLFQSummedFeatureGroup>();

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
                            if (featuresForThisBaseSeqAndFile.Select(p => p.numIdentificationsByBaseSeq).Max() == 1)
                                intensitiesByFile[i] = featuresForThisBaseSeqAndFile.Select(p => (p.intensity / p.numIdentificationsByFullSeq)).Sum();
                            else
                            {
                                intensitiesByFile[i] = 0;
                                //intensitiesByFile[i] = featuresForThisBaseSeqAndFile.Select(p => (p.intensity)).Sum();
                            }
                        }
                    }
                    else
                        identificationType[i] = "";
                }

                returnList.Add(new FlashLFQSummedFeatureGroup(sequence.Key, intensitiesByFile, identificationType));
            }

            return returnList.OrderBy(p => p.BaseSequence);
        }

        private IEnumerable<FlashLFQIsotopeCluster> FilterPeaksByIsotopicDistribution(IEnumerable<FlashLFQMzBinElement> peaks, FlashLFQIdentification identification, int chargeState)
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
                
                /*
                bool badPeak = false;
                double tol = (isotopePpmTolerance / 1e6) * theorIsotopeMz;
                double prevIsotopePeakMz = (mainpeakMz - (1.003322 / chargeState));

                for (int i = thisPeakWithScan.zeroBasedIndexOfPeakInScan; i > 0; i--)
                {
                    var peak = thisPeakWithScan.scan.MassSpectrum[i];
                    if (Math.Abs(peak.Mz - prevIsotopePeakMz) < tol)
                        if ((peak.Intensity / thisPeakWithScan.mainPeak.Intensity > 0.2) && (peak.Intensity / thisPeakWithScan.backgroundIntensity > 5.0))
                            badPeak = true;
                    if (peak.Mz < (prevIsotopePeakMz - tol))
                        break;
                }
                
                if (badPeak)
                    continue;
                */


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
                        else
                        {
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
                        }
                    }
                }

                if (isotopeDistributionCheck)
                {
                    var goodIsotopePeaks = isotopePeaks.Where(p => p != null);
                    isotopeClusters.Add(new FlashLFQIsotopeCluster(thisPeakWithScan, chargeState, goodIsotopePeaks.Select(p => p.Intensity).Sum() - thisPeakWithScan.backgroundIntensity * goodIsotopePeaks.Count()));
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

            //int decreasingIntensityScans = 0;
            //double lastIntensity = 0;

            foreach (var thisPeakWithScan in peaksWithScans)
            {
                ms1IndexHere = ms1ScanNumbers.IndexOf(thisPeakWithScan.oneBasedScanNumber);
                missedScans += Math.Abs(ms1IndexHere - lastGoodIndex) - 1;

                //if (thisPeakWithScan.backgroundSubtractedIntensity < (lastIntensity * 0.5))
                //    decreasingIntensityScans++;
                //else if (thisPeakWithScan.backgroundSubtractedIntensity > lastIntensity)
                //    decreasingIntensityScans = 0;
                
                //if (decreasingIntensityScans >= 2 || (decreasingIntensityScans >= 2 && missedScans > 0))
                //    break;
                if (missedScans > missedScansAllowed)
                    break;

                // found a good peak; reset missed scans to 0
                missedScans = 0;
                lastGoodIndex = ms1IndexHere;
                //lastIntensity = thisPeakWithScan.backgroundSubtractedIntensity;

                validPeaksWithScans.Add(thisPeakWithScan);
            }

            return validPeaksWithScans;
        }
    }
}