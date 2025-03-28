﻿using FlashLFQ;
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
        private static int _fileNameCol;
        private static int _baseSequCol;
        private static int _fullSequCol;
        private static int _monoMassCol;
        private static int _msmsRetnCol;
        private static int _msmsScanCol;
        private static int _chargeStCol;
        private static int _protNameCol;
        private static int _decoyCol;
        private static int _scoreCol;
        private static int _qValueCol;
        private static int _qValueNotchCol;

        // optional columns
        private static int _geneNameCol;

        private static int _organismCol;

        private static Dictionary<string, double> _modSequenceToMonoMass;
        private static Dictionary<string, ProteinGroup> allProteinGroups;
        private static List<ScanHeaderInfo> _scanHeaderInfo = new List<ScanHeaderInfo>();

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

        public static List<Identification> ReadPsms(string filepath, bool silent, List<SpectraFileInfo> rawfiles, double qValueThreshold = 0.01, bool usePepQValue = false)
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

            var psmsGroupedByFile = inputPsms.GroupBy(p => PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(p.Split('\t')[_fileNameCol])).ToList();

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
                                Console.WriteLine("Decoy column set to: " + _decoyCol);

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
                                Console.WriteLine("Decoy column set to: " + _decoyCol);
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
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="silent"></param>
        /// <param name="rawFileDictionary"></param>
        /// <param name="fileType"></param>
        /// <param name="qValueThreshold"> Minimum is 0.01. </param>
        /// <returns></returns>
        private static Identification GetIdentification(string line, bool silent, Dictionary<string, SpectraFileInfo> rawFileDictionary, PsmFileType fileType, double qValueThreshold = 0.01)
        {
            var param = line.Split('\t');
            double qValue= 0;
            qValueThreshold = Math.Max(qValueThreshold, 0.01);

            // only quantify PSMs below 1% FDR with MetaMorpheus/Morpheus results
            if (fileType == PsmFileType.MetaMorpheus)
            {
                qValue = double.Parse(param[_qValueNotchCol], CultureInfo.InvariantCulture);
                if (qValue > qValueThreshold)
                {
                    return null;
                }
            }
            else if (fileType == PsmFileType.Morpheus && double.Parse(param[_qValueCol], CultureInfo.InvariantCulture) > 1.00)
            {
                return null;
            }

            // find and label decoys in MetaMorpheus results
            //TODO: what about decoys from other input types?
            bool decoy = false;
            if ((fileType == PsmFileType.MetaMorpheus || fileType == PsmFileType.Morpheus || fileType == PsmFileType.Generic) 
                && _decoyCol >= 0
                && param[_decoyCol].Contains("D"))
            {
                decoy = true;
            }

            // spectrum file name
            string fileName = PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(param[_fileNameCol]);

            // base sequence
            string baseSequence = param[_baseSequCol];

            // modified sequence
            string modSequence = param[_fullSequCol];

            // skip ambiguous sequence in MetaMorpheus output
            if (fileType == PsmFileType.MetaMorpheus && (modSequence.Contains(" or ") || modSequence.Contains("|") || modSequence.ToLowerInvariant().Contains("too long")))
            {
                return null;
            }

            // monoisotopic mass
            if (double.TryParse(param[_monoMassCol], NumberStyles.Number, CultureInfo.InvariantCulture, out double monoisotopicMass))
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
            if (double.TryParse(param[_msmsRetnCol], NumberStyles.Number, CultureInfo.InvariantCulture, out double retentionTime))
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
                string chargeStringNumbersOnly = new String(param[_chargeStCol].Where(Char.IsDigit).ToArray());

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
                if (!double.TryParse(param[_chargeStCol], NumberStyles.Number, CultureInfo.InvariantCulture, out double chargeStateDouble))
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
            proteins = param[_protNameCol].Split(delimiters[fileType], StringSplitOptions.None);

            if (_geneNameCol >= 0)
            {
                genes = param[_geneNameCol].Split(delimiters[fileType], StringSplitOptions.None);
            }

            if (_organismCol >= 0)
            {
                organisms = param[_organismCol].Split(delimiters[fileType], StringSplitOptions.None);
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
                        gene = param[_geneNameCol];
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
                        organism = param[_organismCol];
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
            if(_scoreCol > 0 && fileType == PsmFileType.MetaMorpheus)
            {
                double.TryParse(param[_scoreCol], out score);
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

        private static Identification GetPercolatorIdentification(string line, List<ScanHeaderInfo> scanHeaderInfo, bool silent, Dictionary<string, SpectraFileInfo> rawFileDictionary)
        {
            var param = line.Split('\t');

            // spectrum file name
            string fileName = param[_fileNameCol];

            // base sequence
            string baseSequence = null;

            // modified sequence
            string modSequence = param[_fullSequCol];

            // skip ambiguous sequence in MetaMorpheus output
            if (modSequence.Contains("|") || modSequence.ToLowerInvariant().Contains("too long"))
            {
                return null;
            }

            // monoisotopic mass
            if (double.TryParse(param[_monoMassCol], NumberStyles.Number, CultureInfo.InvariantCulture, out double monoisotopicMass))
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

            if (int.TryParse(param[_msmsScanCol], NumberStyles.Number, CultureInfo.InvariantCulture, out int scanNumber))
            {
                ms2RetentionTime = scanHeaderInfo.Where(i => PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(i.FileNameWithoutExtension) == PeriodTolerantFilenameWithoutExtension.GetPeriodTolerantFilenameWithoutExtension(fileName) && i.ScanNumber == scanNumber).FirstOrDefault().RetentionTime;
            }

            // charge state
            int chargeState;

            if (!double.TryParse(param[_chargeStCol], NumberStyles.Number, CultureInfo.InvariantCulture, out double chargeStateDouble))
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
            proteins = param[_protNameCol].Split(delimiters[PsmFileType.Percolator], StringSplitOptions.None);

            if (_geneNameCol >= 0)
            {
                genes = param[_geneNameCol].Split(delimiters[PsmFileType.Percolator], StringSplitOptions.None);
            }

            if (_organismCol >= 0)
            {
                organisms = param[_organismCol].Split(delimiters[PsmFileType.Percolator], StringSplitOptions.None);
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
                        gene = param[_geneNameCol];
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
                        organism = param[_organismCol];
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
        private static PsmFileType GetFileTypeFromHeader(string header, bool usePepQValue = false)
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
                _fileNameCol = Array.IndexOf(split, "File Name".ToLowerInvariant());
                _baseSequCol = Array.IndexOf(split, "Base Sequence".ToLowerInvariant());
                _fullSequCol = Array.IndexOf(split, "Full Sequence".ToLowerInvariant());
                _monoMassCol = Array.IndexOf(split, "Peptide Monoisotopic Mass".ToLowerInvariant());
                _msmsRetnCol = Array.IndexOf(split, "Scan Retention Time".ToLowerInvariant());
                _chargeStCol = Array.IndexOf(split, "Precursor Charge".ToLowerInvariant());
                _protNameCol = Array.IndexOf(split, "Protein Accession".ToLowerInvariant());
                _decoyCol = Array.IndexOf(split, "Decoy/Contaminant/Target".ToLowerInvariant());
                _scoreCol = Array.IndexOf(split, "Score".ToLowerInvariant());

                if(usePepQValue)
                {
                    _qValueCol = Array.IndexOf(split, "PEP_QValue".ToLowerInvariant());
                    _qValueNotchCol = Array.IndexOf(split, "PEP_QValue".ToLowerInvariant());
                }
                else
                {
                    _qValueCol = Array.IndexOf(split, "QValue".ToLowerInvariant());
                    _qValueNotchCol = Array.IndexOf(split, "QValue Notch".ToLowerInvariant());
                }
                _geneNameCol = Array.IndexOf(split, "Gene Name".ToLowerInvariant());
                _organismCol = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

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
                _fileNameCol = Array.IndexOf(split, "Filename".ToLowerInvariant());
                _baseSequCol = Array.IndexOf(split, "Base Peptide Sequence".ToLowerInvariant());
                _fullSequCol = Array.IndexOf(split, "Peptide Sequence".ToLowerInvariant());
                _monoMassCol = Array.IndexOf(split, "Theoretical Mass (Da)".ToLowerInvariant());
                _msmsRetnCol = Array.IndexOf(split, "Retention Time (minutes)".ToLowerInvariant());
                _chargeStCol = Array.IndexOf(split, "Precursor Charge".ToLowerInvariant());
                _protNameCol = Array.IndexOf(split, "Protein Description".ToLowerInvariant());
                _decoyCol = Array.IndexOf(split, "Decoy?".ToLowerInvariant());
                _qValueCol = Array.IndexOf(split, "Q-Value (%)".ToLowerInvariant());

                _geneNameCol = Array.IndexOf(split, "Gene Name".ToLowerInvariant()); // probably doesn't exist
                _organismCol = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

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
                _fileNameCol = Array.IndexOf(split, "Raw file".ToLowerInvariant());
                _baseSequCol = Array.IndexOf(split, "Sequence".ToLowerInvariant());
                _fullSequCol = Array.IndexOf(split, "Modified sequence".ToLowerInvariant());
                _monoMassCol = Array.IndexOf(split, "Mass".ToLowerInvariant());
                _msmsRetnCol = Array.IndexOf(split, "Retention time".ToLowerInvariant());
                _chargeStCol = Array.IndexOf(split, "Charge".ToLowerInvariant());
                _protNameCol = Array.IndexOf(split, "Proteins".ToLowerInvariant());
                _geneNameCol = Array.IndexOf(split, "Gene Names".ToLowerInvariant());

                _organismCol = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

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
                _fileNameCol = Array.IndexOf(split, "Spectrum File".ToLowerInvariant());
                _baseSequCol = Array.IndexOf(split, "Sequence".ToLowerInvariant());
                _fullSequCol = Array.IndexOf(split, "Modified Sequence".ToLowerInvariant());
                _monoMassCol = Array.IndexOf(split, "Theoretical Mass".ToLowerInvariant());
                _msmsRetnCol = Array.IndexOf(split, "RT".ToLowerInvariant());
                _chargeStCol = Array.IndexOf(split, "Identification Charge".ToLowerInvariant());
                _protNameCol = Array.IndexOf(split, "Protein(s)".ToLowerInvariant());

                _geneNameCol = Array.IndexOf(split, "Gene Name".ToLowerInvariant()); // probably doesn't exist
                _organismCol = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

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
                _fileNameCol = Array.IndexOf(split, "file_idx".ToLowerInvariant());
                _fullSequCol = Array.IndexOf(split, "sequence".ToLowerInvariant());
                _monoMassCol = Array.IndexOf(split, "peptide mass".ToLowerInvariant()); //TODO: see if this needs to be theoretical or experimental mass AND if it is neutral or monoisotopic(H+)
                _msmsScanCol = Array.IndexOf(split, "scan".ToLowerInvariant());
                _chargeStCol = Array.IndexOf(split, "charge".ToLowerInvariant());
                _protNameCol = Array.IndexOf(split, "protein id".ToLowerInvariant());
                _qValueCol = Array.IndexOf(split, "percolator q-value".ToLowerInvariant());

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
                _fileNameCol = Array.IndexOf(split, "File Name".ToLowerInvariant());
                _baseSequCol = Array.IndexOf(split, "Base Sequence".ToLowerInvariant());
                _fullSequCol = Array.IndexOf(split, "Full Sequence".ToLowerInvariant());
                _monoMassCol = Array.IndexOf(split, "Peptide Monoisotopic Mass".ToLowerInvariant());
                _msmsRetnCol = Array.IndexOf(split, "Scan Retention Time".ToLowerInvariant());
                _chargeStCol = Array.IndexOf(split, "Precursor Charge".ToLowerInvariant());
                _protNameCol = Array.IndexOf(split, "Protein Accession".ToLowerInvariant());

                _geneNameCol = Array.IndexOf(split, "Gene Name".ToLowerInvariant()); // probably doesn't exist
                _organismCol = Array.IndexOf(split, "Organism Name".ToLowerInvariant());

                _decoyCol = Array.IndexOf(split, "Target/Decoy".ToLowerInvariant());

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