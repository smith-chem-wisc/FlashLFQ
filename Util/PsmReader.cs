using FlashLFQ;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UsefulProteomicsDatabases;

namespace Util
{
    internal enum PsmFileType
    { MetaMorpheus, Morpheus, MaxQuant, PeptideShaker, Generic, Percolator, Unknown }

    public class PsmReader
    {

        public PsmReader() 
        { 
            //Set optional columns to -1
            GeneNameColumn = -1;
            OrganismColumn = -1;
            DecoyColumn = -1;
            ScoreColumn = -1;
            QValueColumn = -1;
            QValueNotchColumn = -1;
        }

        public int FileNameColumn { get; private set; }
        public int BaseSeqColumn { get; private set; }
        public int FullSeqColumn { get; private set; }
        public int MonoisotopicMassColumn { get; private set; }
        public int MsmsRTColumn { get; private set; }
        public int MsmsScanNumberColumn { get; private set; }
        public int ChargeStateColumn { get; private set; }
        public int ProteinColumn { get; private set; }

        // optional columns
        public int GeneNameColumn { get; private set; }
        public int OrganismColumn { get; private set; }
        public int DecoyColumn { get; private set; }
        public int ScoreColumn { get; private set; }
        public static int QValueColumn { get; private set; }
        public static int QValueNotchColumn { get; private set; }

        private Dictionary<string, double> _modSequenceToMonoMass;
        private Dictionary<string, ProteinGroup> allProteinGroups;
        private List<ScanHeaderInfo> _scanHeaderInfo = new List<ScanHeaderInfo>();

        //Delimiters refere to contents of one field, not the delimiter between fields
        private static readonly Dictionary<PsmFileType, string[]> delimiters = new Dictionary<PsmFileType, string[]>
        {
            { PsmFileType.MetaMorpheus, new string[] { "|", " or " } },
            { PsmFileType.Morpheus, new string[] { ";" } },
            { PsmFileType.MaxQuant, new string[] { ";" } },
            { PsmFileType.Percolator, new string[] { "," } },
            { PsmFileType.Generic, new string[] { ";" } },
            { PsmFileType.PeptideShaker, new string[] { ", " } },
        };

        public List<Identification> ReadPsms(string filepath, bool silent, List<SpectraFileInfo> rawfiles, double qValueThreshold = 0.01, bool usePepQValue = false)
        {
            if (_modSequenceToMonoMass == null)
            {
                _modSequenceToMonoMass = new Dictionary<string, double>();
            }

            if (allProteinGroups == null)
            {
                allProteinGroups = new Dictionary<string, ProteinGroup>();
            }

            var rawFileDictionary = rawfiles.ToDictionary(p => p.FilenameWithoutExtension, v => v);
            List<Identification> flashLfqIdentifications = new();
            PsmFileType fileType = PsmFileType.Unknown;

            if (!silent)
            {
                Console.WriteLine("Opening PSM file " + filepath);
            }

            StreamReader reader;
            try
            {
                reader = new StreamReader(filepath);
            }
            catch (Exception e)
            {
                if (!silent)
                {
                    Console.WriteLine("Error reading file " + filepath + "\n" + e.Message);
                }

                return new List<Identification>();
            }

            List<string> inputPsms = File.ReadAllLines(filepath).ToList();
            string[] header = inputPsms[0].Split('\t');

            try
            {
                fileType = GetFileTypeFromHeader(inputPsms[0], usePepQValue);
                inputPsms.RemoveAt(0);
            }
            catch
            {
                throw new Exception("Could not interpret PSM header labels from file: " + filepath);
            }

            var psmsGroupedByFile = inputPsms.GroupBy(p => PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(p.Split('\t')[FileNameColumn])).ToList();

            foreach (var fileSpecificPsms in psmsGroupedByFile)
            {
                int myFileIndex = rawFileDictionary.Keys.ToList().IndexOf(fileSpecificPsms.Key);
                string fullFilePathWithExtension = rawFileDictionary[fileSpecificPsms.Key].FullFilePathWithExtension;
                List<Identification> myFileIndentifications = new();
                List<ScanHeaderInfo> scanHeaderInfo = new();
                if (fileType == PsmFileType.Percolator)
                {
                    scanHeaderInfo = scanHeaderInfo = ScanInfoRecovery.FileScanHeaderInfo(fullFilePathWithExtension);
                    foreach (var psm in fileSpecificPsms)
                    {
                        try
                        {
                            Identification id = GetPercolatorIdentification(psm, scanHeaderInfo, silent, rawFileDictionary);
                            if (id != null)
                            {
                                myFileIndentifications.Add(id);
                            }
                        }
                        catch (Exception e)
                        {
                            if (!silent)
                            {
                                Console.WriteLine("Problem reading line in the identification file" + "; " + e.Message);
                                Console.WriteLine("Decoy column set to: " + DecoyColumn);

                            }
                        }
                    }
                }
                else
                {
                    foreach (var psm in fileSpecificPsms)
                    {
                        try
                        {
                            Identification id = GetIdentification(psm, silent, rawFileDictionary, fileType, qValueThreshold);
                            if (id != null)
                            {
                                myFileIndentifications.Add(id);
                            }
                        }
                        catch (Exception e)
                        {
                            if (!silent)
                            {
                                Console.WriteLine("Problem reading line in the identification file" + "; " + e.Message);
                                Console.WriteLine("Decoy column set to: " + DecoyColumn);
                            }
                        }
                    }
                }

                _scanHeaderInfo.AddRange(scanHeaderInfo);
                flashLfqIdentifications.AddRange(myFileIndentifications);
            }

            if (!silent)
            {
                Console.WriteLine("Done reading PSMs; found " + flashLfqIdentifications.Count);
            }

            return flashLfqIdentifications;
        }

        /// <summary>
        /// Retrieves an individual ID from a line of a PSM file
        /// </summary>
        /// <param name="line"></param>
        /// <param name="silent"></param>
        /// <param name="rawFileDictionary"></param>
        /// <param name="fileType"></param>
        /// <param name="qValueThreshold"> Minimum is 0.01. </param>
        /// <returns></returns>
        private Identification GetIdentification(string line, bool silent, Dictionary<string, SpectraFileInfo> rawFileDictionary, PsmFileType fileType, double qValueThreshold = 0.01)
        {
            var param = line.Split('\t');
            double qValue= 0;
            qValueThreshold = Math.Max(qValueThreshold, 0.01);

            // only quantify PSMs below the qValueThreshold with MetaMorpheus/Morpheus/Generic results
            switch (fileType)
            {
                case (PsmFileType.MetaMorpheus):
                    qValue = double.Parse(param[QValueNotchColumn], CultureInfo.InvariantCulture);
                    if (qValue > qValueThreshold)
                        return null;
                    break;
                case (PsmFileType.Morpheus): // This is legacy code, I have no idea how Morpheus files work or why Q values would be greater than 1
                    if (double.Parse(param[QValueColumn], CultureInfo.InvariantCulture) > 1.00)
                        return null;
                    break;
                default:
                    if (QValueColumn < 0)
                        break;
                    qValue = double.Parse(param[QValueColumn], CultureInfo.InvariantCulture);
                    if (qValue > qValueThreshold)
                        return null;
                    break;
            }

            // find and label decoys in MetaMorpheus results
            //TODO: what about decoys from other input types?
            bool decoy = ((fileType == PsmFileType.MetaMorpheus || fileType == PsmFileType.Morpheus || fileType == PsmFileType.Generic)
                && DecoyColumn >= 0
                && param[DecoyColumn].Contains('D'));

            // spectrum file name
            string fileName = PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(param[FileNameColumn]);

            // base sequence
            string baseSequence = param[BaseSeqColumn];

            // modified sequence
            string modSequence = param[FullSeqColumn];

            // skip ambiguous sequence in MetaMorpheus output
            if (fileType == PsmFileType.MetaMorpheus && (modSequence.Contains(" or ") || modSequence.Contains("|") || modSequence.ToLowerInvariant().Contains("too long")))
            {
                return null;
            }

            // monoisotopic mass
            if (double.TryParse(param[MonoisotopicMassColumn], NumberStyles.Number, CultureInfo.InvariantCulture, out double monoisotopicMass))
            {
                if (_modSequenceToMonoMass.TryGetValue(modSequence, out double storedMonoisotopicMass))
                {
                    if (storedMonoisotopicMass != monoisotopicMass)
                    {
                        if (!silent)
                        {
                            Console.WriteLine("Caution! PSM with could not be read. A peptide with the same modified sequence but a different monoisotopic mass has already been added." + "\n"
                                + line);
                        }

                        return null;
                    }
                }
                else
                {
                    _modSequenceToMonoMass.Add(modSequence, monoisotopicMass);
                }
            }
            else
            {
                if (!silent)
                {
                    Console.WriteLine("Caution! PSM with could not be read. Monoisotopic mass not interpretable." + "\n"
                                + line);
                }

                return null;
            }

            // retention time
            double ms2RetentionTime = -1;
            if (double.TryParse(param[MsmsRTColumn], NumberStyles.Number, CultureInfo.InvariantCulture, out double retentionTime))
            {
                ms2RetentionTime = retentionTime;
                if (fileType == PsmFileType.PeptideShaker)
                {
                    // peptide shaker RT is in seconds - convert to minutes
                    ms2RetentionTime = retentionTime / 60.0;
                }

                if (ms2RetentionTime < 0)
                {
                    if (!silent)
                    {
                        Console.WriteLine("Caution! PSM retention time was negative." + "\n"
                            + line);
                    }

                    return null;
                }
            }
            else
            {
                if (!silent)
                {
                    Console.WriteLine("PSM retention time was not interpretable." + "\n"
                        + line);
                }

                return null;
            }

            // charge state
            int chargeState;
            if (fileType == PsmFileType.PeptideShaker)
            {
                string chargeStringNumbersOnly = new String(param[ChargeStateColumn].Where(Char.IsDigit).ToArray());

                if (string.IsNullOrWhiteSpace(chargeStringNumbersOnly))
                {
                    if (!silent)
                    {
                        Console.WriteLine("PSM charge state was not interpretable." + "\n"
                            + line);
                    }

                    return null;
                }
                else
                {
                    if (!int.TryParse(chargeStringNumbersOnly, out chargeState))
                    {
                        if (!silent)
                        {
                            Console.WriteLine("PSM charge state was not interpretable." + "\n"
                            + line);
                        }

                        return null;
                    }
                }
            }
            else
            {
                if (!double.TryParse(param[ChargeStateColumn], NumberStyles.Number, CultureInfo.InvariantCulture, out double chargeStateDouble))
                {
                    if (!silent)
                    {
                        Console.WriteLine("PSM charge state was not interpretable." + "\n"
                            + line);
                    }

                    return null;
                }

                chargeState = (int)chargeStateDouble;
            }

            // protein groups
            // use all proteins listed
            string[] proteins = null;
            string[] genes = null;
            string[] organisms = null;
            List<ProteinGroup> proteinGroups = new List<ProteinGroup>();
            proteins = param[ProteinColumn].Split(delimiters[fileType], StringSplitOptions.None);

            if (GeneNameColumn >= 0)
            {
                genes = param[GeneNameColumn].Split(delimiters[fileType], StringSplitOptions.None);
            }

            if (OrganismColumn >= 0)
            {
                organisms = param[OrganismColumn].Split(delimiters[fileType], StringSplitOptions.None);
            }

            for (int pr = 0; pr < proteins.Length; pr++)
            {
                string proteinName = proteins[pr];
                string gene = "";
                string organism = "";

                if (genes != null)
                {
                    if (genes.Length == 1)
                    {
                        gene = genes[0];
                    }
                    else if (genes.Length == proteins.Length)
                    {
                        gene = genes[pr];
                    }
                    else if (proteins.Length == 1)
                    {
                        gene = param[GeneNameColumn];
                    }
                }

                if (organisms != null)
                {
                    if (organisms.Length == 1)
                    {
                        organism = organisms[0];
                    }
                    else if (organisms.Length == proteins.Length)
                    {
                        organism = organisms[pr];
                    }
                    else if (proteins.Length == 1)
                    {
                        organism = param[OrganismColumn];
                    }
                }

                if (allProteinGroups.TryGetValue(proteinName, out ProteinGroup pg))
                {
                    proteinGroups.Add(pg);
                }
                else
                {
                    ProteinGroup newPg = new ProteinGroup(proteinName, gene, organism);
                    allProteinGroups.Add(proteinName, newPg);
                    proteinGroups.Add(newPg);
                }
            }

            if (!rawFileDictionary.TryGetValue(fileName, out SpectraFileInfo spectraFileInfoToUse))
            {
                // skip PSMs for files with no spectrum data input
                return null;
            }

            double score;
            if(ScoreColumn > 0 && fileType == PsmFileType.MetaMorpheus)
            {
                double.TryParse(param[ScoreColumn], out score);
            }
            else
            {
                score = 0;
            }

            // construct id
            return new Identification(spectraFileInfoToUse, baseSequence, modSequence, 
                monoisotopicMass, ms2RetentionTime, chargeState, proteinGroups, 
                decoy: decoy, qValue: qValue, psmScore: score);
        }

        private Identification GetPercolatorIdentification(string line, List<ScanHeaderInfo> scanHeaderInfo, bool silent, Dictionary<string, SpectraFileInfo> rawFileDictionary)
        {
            var param = line.Split('\t');

            // spectrum file name
            string fileName = param[FileNameColumn];

            // base sequence
            string baseSequence = null;

            // modified sequence
            string modSequence = param[FullSeqColumn];

            // skip ambiguous sequence in MetaMorpheus output
            if (modSequence.Contains("|") || modSequence.ToLowerInvariant().Contains("too long"))
            {
                return null;
            }

            // monoisotopic mass
            if (double.TryParse(param[MonoisotopicMassColumn], NumberStyles.Number, CultureInfo.InvariantCulture, out double monoisotopicMass))
            {
                if (_modSequenceToMonoMass.TryGetValue(modSequence, out double storedMonoisotopicMass))
                {
                    if (storedMonoisotopicMass != monoisotopicMass)
                    {
                        if (!silent)
                        {
                            Console.WriteLine("Caution! PSM with could not be read. A peptide with the same modified sequence but a different monoisotopic mass has already been added." + "\n"
                                + line);
                        }
                        return null;
                    }
                }
                else
                {
                    _modSequenceToMonoMass.Add(modSequence, monoisotopicMass);
                }
            }
            else
            {
                if (!silent)
                {
                    Console.WriteLine("Caution! PSM with could not be read. Monoisotopic mass not interpretable." + "\n"
                                + line);
                }
                return null;
            }

            // retention time
            double ms2RetentionTime = -1;
            //percolator input files do not have retention times. So, we have to get them from the data file using the scan number.

            if (int.TryParse(param[MsmsScanNumberColumn], NumberStyles.Number, CultureInfo.InvariantCulture, out int scanNumber))
            {
                ms2RetentionTime = scanHeaderInfo.Where(i => PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(i.FileNameWithoutExtension) == PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(fileName) && i.ScanNumber == scanNumber).FirstOrDefault().RetentionTime;
            }

            // charge state
            int chargeState;

            if (!double.TryParse(param[ChargeStateColumn], NumberStyles.Number, CultureInfo.InvariantCulture, out double chargeStateDouble))
            {
                if (!silent)
                {
                    Console.WriteLine("Caution! PSM with could not be read. Charge state not interpretable." + "\n"
                            + line);
                }

                return null;
            }

            chargeState = (int)chargeStateDouble;

            // protein groups
            // use all proteins listed
            string[] proteins = null;
            string[] genes = null;
            string[] organisms = null;
            List<ProteinGroup> proteinGroups = new List<ProteinGroup>();
            proteins = param[ProteinColumn].Split(delimiters[PsmFileType.Percolator], StringSplitOptions.None);

            if (GeneNameColumn >= 0)
            {
                genes = param[GeneNameColumn].Split(delimiters[PsmFileType.Percolator], StringSplitOptions.None);
            }

            if (OrganismColumn >= 0)
            {
                organisms = param[OrganismColumn].Split(delimiters[PsmFileType.Percolator], StringSplitOptions.None);
            }

            for (int pr = 0; pr < proteins.Length; pr++)
            {
                string proteinName = proteins[pr];
                string gene = "";
                string organism = "";

                if (genes != null)
                {
                    if (genes.Length == 1)
                    {
                        gene = genes[0];
                    }
                    else if (genes.Length == proteins.Length)
                    {
                        gene = genes[pr];
                    }
                    else if (proteins.Length == 1)
                    {
                        gene = param[GeneNameColumn];
                    }
                }

                if (organisms != null)
                {
                    if (organisms.Length == 1)
                    {
                        organism = organisms[0];
                    }
                    else if (organisms.Length == proteins.Length)
                    {
                        organism = organisms[pr];
                    }
                    else if (proteins.Length == 1)
                    {
                        organism = param[OrganismColumn];
                    }
                }

                if (allProteinGroups.TryGetValue(proteinName, out ProteinGroup pg))
                {
                    proteinGroups.Add(pg);
                }
                else
                {
                    ProteinGroup newPg = new ProteinGroup(proteinName, gene, organism);
                    allProteinGroups.Add(proteinName, newPg);
                    proteinGroups.Add(newPg);
                }
            }

            if (!rawFileDictionary.TryGetValue(PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(fileName), out SpectraFileInfo spectraFileInfoToUse))
            {
                // skip PSMs for files with no spectrum data input
                return null;
            }

            return new Identification(spectraFileInfoToUse, baseSequence, modSequence, monoisotopicMass, ms2RetentionTime, chargeState, proteinGroups);
        }

        /// <summary>
        /// In addition to determining the file type based off of the header, this also sets the column indices for all the fields 
        /// that will be read when reading in the Identificatoin
        /// </summary>
        private PsmFileType GetFileTypeFromHeader(string header, bool usePepQValue = false)
        {
            PsmFileType type = PsmFileType.Unknown;

            var split = header.Split('\t').Select(p => p.ToLowerInvariant()).ToArray();

            // MetaMorpheus MS/MS input
            if (split.Contains("File Name".ToLowerInvariant())
                        && split.Contains("Base Sequence".ToLowerInvariant())
                        && split.Contains("Full Sequence".ToLowerInvariant())
                        && split.Contains("Peptide Monoisotopic Mass".ToLowerInvariant())
                        && split.Contains("Scan Retention Time".ToLowerInvariant())
                        && split.Contains("Precursor Charge".ToLowerInvariant())
                        && split.Contains("Protein Accession".ToLowerInvariant())
                        && split.Contains("Decoy/Contaminant/Target".ToLowerInvariant())
                        && split.Contains("QValue".ToLowerInvariant())
                        && split.Contains("QValue Notch".ToLowerInvariant()))
            {
                FileNameColumn = Array.IndexOf(split, "File Name".ToLowerInvariant());
                BaseSeqColumn = Array.IndexOf(split, "Base Sequence".ToLowerInvariant());
                FullSeqColumn = Array.IndexOf(split, "Full Sequence".ToLowerInvariant());
                MonoisotopicMassColumn = Array.IndexOf(split, "Peptide Monoisotopic Mass".ToLowerInvariant());
                MsmsRTColumn = Array.IndexOf(split, "Scan Retention Time".ToLowerInvariant());
                ChargeStateColumn = Array.IndexOf(split, "Precursor Charge".ToLowerInvariant());
                ProteinColumn = Array.IndexOf(split, "Protein Accession".ToLowerInvariant());
                DecoyColumn = Array.IndexOf(split, "Decoy/Contaminant/Target".ToLowerInvariant());
                ScoreColumn = Array.IndexOf(split, "Score".ToLowerInvariant());

                if(usePepQValue)
                {
                    QValueColumn = Array.IndexOf(split, "PEP_QValue".ToLowerInvariant());
                    QValueNotchColumn = Array.IndexOf(split, "PEP_QValue".ToLowerInvariant());
                }
                else
                {
                    QValueColumn = Array.IndexOf(split, "QValue".ToLowerInvariant());
                    QValueNotchColumn = Array.IndexOf(split, "QValue Notch".ToLowerInvariant());
                }
                GeneNameColumn = Array.IndexOf(split, "Gene Name".ToLowerInvariant());
                OrganismColumn = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

                return PsmFileType.MetaMorpheus;
            }

            // Morpheus MS/MS input
            else if (split.Contains("Filename".ToLowerInvariant())
                && split.Contains("Base Peptide Sequence".ToLowerInvariant())
                && split.Contains("Peptide Sequence".ToLowerInvariant())
                && split.Contains("Theoretical Mass (Da)".ToLowerInvariant())
                && split.Contains("Retention Time (minutes)".ToLowerInvariant())
                && split.Contains("Precursor Charge".ToLowerInvariant())
                && split.Contains("Protein Description".ToLowerInvariant())
                && split.Contains("Decoy?".ToLowerInvariant())
                && split.Contains("Q-Value (%)".ToLowerInvariant()))
            {
                FileNameColumn = Array.IndexOf(split, "Filename".ToLowerInvariant());
                BaseSeqColumn = Array.IndexOf(split, "Base Peptide Sequence".ToLowerInvariant());
                FullSeqColumn = Array.IndexOf(split, "Peptide Sequence".ToLowerInvariant());
                MonoisotopicMassColumn = Array.IndexOf(split, "Theoretical Mass (Da)".ToLowerInvariant());
                MsmsRTColumn = Array.IndexOf(split, "Retention Time (minutes)".ToLowerInvariant());
                ChargeStateColumn = Array.IndexOf(split, "Precursor Charge".ToLowerInvariant());
                ProteinColumn = Array.IndexOf(split, "Protein Description".ToLowerInvariant());
                DecoyColumn = Array.IndexOf(split, "Decoy?".ToLowerInvariant());
                QValueColumn = Array.IndexOf(split, "Q-Value (%)".ToLowerInvariant());

                GeneNameColumn = Array.IndexOf(split, "Gene Name".ToLowerInvariant()); // probably doesn't exist
                OrganismColumn = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

                return PsmFileType.Morpheus;
            }

            // MaxQuant MS/MS input
            else if (split.Contains("Raw file".ToLowerInvariant())
                && split.Contains("Sequence".ToLowerInvariant())
                && split.Contains("Modified sequence".ToLowerInvariant())
                && split.Contains("Mass".ToLowerInvariant())
                && split.Contains("Retention time".ToLowerInvariant())
                && split.Contains("Charge".ToLowerInvariant())
                && split.Contains("Proteins".ToLowerInvariant()))
            {
                FileNameColumn = Array.IndexOf(split, "Raw file".ToLowerInvariant());
                BaseSeqColumn = Array.IndexOf(split, "Sequence".ToLowerInvariant());
                FullSeqColumn = Array.IndexOf(split, "Modified sequence".ToLowerInvariant());
                MonoisotopicMassColumn = Array.IndexOf(split, "Mass".ToLowerInvariant());
                MsmsRTColumn = Array.IndexOf(split, "Retention time".ToLowerInvariant());
                ChargeStateColumn = Array.IndexOf(split, "Charge".ToLowerInvariant());
                ProteinColumn = Array.IndexOf(split, "Proteins".ToLowerInvariant());
                GeneNameColumn = Array.IndexOf(split, "Gene Names".ToLowerInvariant());

                OrganismColumn = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

                return PsmFileType.MaxQuant;
            }

            // Peptide Shaker Input
            else if (split.Contains("Spectrum File".ToLowerInvariant())
                && split.Contains("Sequence".ToLowerInvariant())
                && split.Contains("Modified Sequence".ToLowerInvariant())
                && split.Contains("Theoretical Mass".ToLowerInvariant())
                && split.Contains("RT".ToLowerInvariant())
                && split.Contains("Identification Charge".ToLowerInvariant())
                && split.Contains("Protein(s)".ToLowerInvariant()))
            {
                FileNameColumn = Array.IndexOf(split, "Spectrum File".ToLowerInvariant());
                BaseSeqColumn = Array.IndexOf(split, "Sequence".ToLowerInvariant());
                FullSeqColumn = Array.IndexOf(split, "Modified Sequence".ToLowerInvariant());
                MonoisotopicMassColumn = Array.IndexOf(split, "Theoretical Mass".ToLowerInvariant());
                MsmsRTColumn = Array.IndexOf(split, "RT".ToLowerInvariant());
                ChargeStateColumn = Array.IndexOf(split, "Identification Charge".ToLowerInvariant());
                ProteinColumn = Array.IndexOf(split, "Protein(s)".ToLowerInvariant());

                GeneNameColumn = Array.IndexOf(split, "Gene Name".ToLowerInvariant()); // probably doesn't exist
                OrganismColumn = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

                return PsmFileType.PeptideShaker;
            }

            // Percolator Input
            // Assume that no decoy are provided in this input
            else if (split.Contains("file_idx".ToLowerInvariant())
                && split.Contains("scan".ToLowerInvariant())
                && split.Contains("charge".ToLowerInvariant())
                && split.Contains("spectrum neutral mass".ToLowerInvariant()) //experimental neutral mass
                && split.Contains("peptide mass".ToLowerInvariant()) //theoretical neutral (uncharged) peptide mass
                && split.Contains("sequence".ToLowerInvariant())
                && split.Contains("protein id".ToLowerInvariant()))
            {
                FileNameColumn = Array.IndexOf(split, "file_idx".ToLowerInvariant());
                FullSeqColumn = Array.IndexOf(split, "sequence".ToLowerInvariant());
                MonoisotopicMassColumn = Array.IndexOf(split, "peptide mass".ToLowerInvariant()); //TODO: see if this needs to be theoretical or experimental mass AND if it is neutral or monoisotopic(H+)
                MsmsScanNumberColumn = Array.IndexOf(split, "scan".ToLowerInvariant());
                ChargeStateColumn = Array.IndexOf(split, "charge".ToLowerInvariant());
                ProteinColumn = Array.IndexOf(split, "protein id".ToLowerInvariant());
                QValueColumn = Array.IndexOf(split, "percolator q-value".ToLowerInvariant());

                return PsmFileType.Percolator;
            }

            // Generic MS/MS input
            if (split.Contains("File Name".ToLowerInvariant())
                        && split.Contains("Base Sequence".ToLowerInvariant())
                        && split.Contains("Full Sequence".ToLowerInvariant())
                        && split.Contains("Peptide Monoisotopic Mass".ToLowerInvariant())
                        && split.Contains("Scan Retention Time".ToLowerInvariant())
                        && split.Contains("Precursor Charge".ToLowerInvariant())
                        && split.Contains("Protein Accession".ToLowerInvariant()))
            {
                FileNameColumn = Array.IndexOf(split, "File Name".ToLowerInvariant());
                BaseSeqColumn = Array.IndexOf(split, "Base Sequence".ToLowerInvariant());
                FullSeqColumn = Array.IndexOf(split, "Full Sequence".ToLowerInvariant());
                MonoisotopicMassColumn = Array.IndexOf(split, "Peptide Monoisotopic Mass".ToLowerInvariant());
                MsmsRTColumn = Array.IndexOf(split, "Scan Retention Time".ToLowerInvariant());
                ChargeStateColumn = Array.IndexOf(split, "Precursor Charge".ToLowerInvariant());
                ProteinColumn = Array.IndexOf(split, "Protein Accession".ToLowerInvariant());

                GeneNameColumn = Array.IndexOf(split, "Gene Name".ToLowerInvariant()); // probably doesn't exist
                OrganismColumn = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

                QValueColumn = Array.IndexOf(split, "Q-Value".ToLowerInvariant());
                DecoyColumn = Array.IndexOf(split, "Target/Decoy".ToLowerInvariant());

                return PsmFileType.Generic;
            }

            return type;
        }

        private static string ApplyRegex(FastaHeaderFieldRegex regex, string line)
        {
            string result = null;
            if (regex != null)
            {
                var matches = regex.Regex.Matches(line);
                if (matches.Count > regex.Match && matches[regex.Match].Groups.Count > regex.Group)
                {
                    result = matches[regex.Match].Groups[regex.Group].Value;
                }
            }
            return result;
        }
    }
}