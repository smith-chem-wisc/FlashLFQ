using FlashLFQ;
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
            { PsmFileType.Percolator, new string[] { "|", "," } },
            { PsmFileType.Generic, new string[] { ";" } },
            { PsmFileType.PeptideShaker, new string[] { ", " } },
        };

        public static List<Identification> ReadPsms(string filepath, bool silent, List<SpectraFileInfo> rawfiles)
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
            List<Identification> ids = new List<Identification>();
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

            int lineNum = 0;

            while (reader.Peek() > 0)
            {
                string line = reader.ReadLine();
                lineNum++;

                try
                {
                    if (lineNum == 1)
                    {
                        fileType = GetFileTypeFromHeader(line);

                        if (fileType == PsmFileType.Unknown)
                        {
                            throw new Exception("Could not interpret PSM header labels from file: " + filepath);
                        }
                        if (fileType == PsmFileType.Percolator)
                        {
                            //Percolator files are missing retention times. So we have to load them ahead of time. Dynamic scan access is time cost prohibitive.
                            foreach (KeyValuePair<string, SpectraFileInfo> item in rawFileDictionary)
                            {
                                _scanHeaderInfo.AddRange(ScanInfoRecovery.FileScanHeaderInfo(item.Value.FullFilePathWithExtension));
                            }
                        }
                    }
                    else
                    {
                        var param = line.Split('\t');

                        // only quantify PSMs below 1% FDR with MetaMorpheus/Morpheus results
                        if (fileType == PsmFileType.MetaMorpheus && double.Parse(param[_qValueCol], CultureInfo.InvariantCulture) > 0.01)
                        {
                            break;
                        }
                        else if (fileType == PsmFileType.Morpheus && double.Parse(param[_qValueCol], CultureInfo.InvariantCulture) > 1.00)
                        {
                            break;
                        }

                        // only quantify PSMs below 1% notch FDR with MetaMorpheus/Morpheus results
                        if (fileType == PsmFileType.MetaMorpheus && double.Parse(param[_qValueNotchCol], CultureInfo.InvariantCulture) > 0.01)
                        {
                            continue;
                        }

                        // skip decoys with MetaMorpheus/Morpheus results
                        //TODO: what about decoys from other input types?
                        if ((fileType == PsmFileType.MetaMorpheus || fileType == PsmFileType.Morpheus) &&
                            param[_decoyCol].Contains("D"))
                        {
                            continue;
                        }

                        // spectrum file name
                        string fileName = param[_fileNameCol];

                        // base sequence
                        string baseSequence = null;
                        if (fileType != PsmFileType.Percolator)
                        {
                            baseSequence = null;
                        }

                        // modified sequence
                        string modSequence = param[_fullSequCol];

                        // skip ambiguous sequence in MetaMorpheus output
                        if (fileType == PsmFileType.MetaMorpheus && (modSequence.Contains(" or ") || modSequence.Contains("|") || modSequence.ToLowerInvariant().Contains("too long")))
                        {
                            continue;
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
                                        Console.WriteLine("Caution! PSM with sequence " + modSequence + " at line " +
                                           lineNum + " could not be read; " +
                                           "a peptide with the same modified sequence but a different monoisotopic mass has already been added");
                                    }

                                    continue;
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
                                Console.WriteLine("PSM with sequence " + modSequence + " at line " +
                                   lineNum + " could not be read; " +
                                   "monoisotopic mass was not interpretable: \"" + param[_monoMassCol] + "\"");
                            }

                            continue;
                        }

                        // retention time
                        double ms2RetentionTime = -1;
                        //percolator input files do not have retention times. So, we have to get them from the data file using the scan number.
                        if (fileType == PsmFileType.Percolator)
                        {
                            if (int.TryParse(param[_msmsScanCol], NumberStyles.Number, CultureInfo.InvariantCulture, out int scanNumber))
                            {
                                ms2RetentionTime = _scanHeaderInfo.Where(i => Path.GetFileNameWithoutExtension(i.FileName) == Path.GetFileNameWithoutExtension(fileName) && i.ScanNumber == scanNumber).FirstOrDefault().RetentionTime;
                            }
                        }
                        else if (double.TryParse(param[_msmsRetnCol], NumberStyles.Number, CultureInfo.InvariantCulture, out double retentionTime))
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
                                    Console.WriteLine("Caution! PSM with sequence " + modSequence + " at line " +
                                       lineNum + " could not be read; retention time was negative");
                                }

                                continue;
                            }
                        }
                        else
                        {
                            if (!silent)
                            {
                                Console.WriteLine("PSM with sequence " + modSequence + " at line " +
                                   lineNum + " could not be read; " +
                                   "retention time was not interpretable: \"" + param[_msmsRetnCol] + "\"");
                            }

                            continue;
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
                                    Console.WriteLine("PSM with sequence " + modSequence + " at line " +
                                        lineNum + " could not be read; " +
                                        "charge state was not interpretable: \"" + param[_chargeStCol] + "\"");
                                }

                                continue;
                            }
                            else
                            {
                                if (!int.TryParse(chargeStringNumbersOnly, out chargeState))
                                {
                                    if (!silent)
                                    {
                                        Console.WriteLine("PSM with sequence " + modSequence + " at line " +
                                            lineNum + " could not be read; " +
                                            "charge state was not interpretable: \"" + param[_chargeStCol] + "\"");
                                    }

                                    continue;
                                }
                            }
                        }
                        else
                        {
                            if (!double.TryParse(param[_chargeStCol], NumberStyles.Number, CultureInfo.InvariantCulture, out double chargeStateDouble))
                            {
                                if (!silent)
                                {
                                    Console.WriteLine("PSM with sequence " + modSequence + " at line " +
                                        lineNum + " could not be read; " +
                                        "charge state was not interpretable: \"" + param[_chargeStCol] + "\"");
                                }

                                continue;
                            }

                            chargeState = (int)chargeStateDouble;
                        }

                        // protein groups
                        // use all proteins listed
                        string[] proteins = null;
                        string[] genes = null;
                        string[] organisms = null;
                        List<ProteinGroup> proteinGroups = new List<ProteinGroup>();

                        if (fileType == PsmFileType.Percolator)
                        {
                            string[] proteinFastaHeaders = param[_protNameCol].Split(',');
                            foreach (string fastHeader in proteinFastaHeaders)
                            {
                                string[] fastaHeaderFields = fastHeader.Split('|');
                                string accession = fastaHeaderFields[1];
                                string geneNameString = fastaHeaderFields[2];

                                if (allProteinGroups.TryGetValue(accession, out ProteinGroup pg))
                                {
                                    proteinGroups.Add(pg);
                                }
                                else
                                {
                                    ProteinGroup newPg = new ProteinGroup(accession, geneNameString, null);
                                    allProteinGroups.Add(accession, newPg);
                                    proteinGroups.Add(newPg);
                                }

                            }
                        }
                        else
                        {
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
                        }

                        // get file name and look up file name object
                        var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);

                        if (!rawFileDictionary.TryGetValue(fileNameNoExt, out SpectraFileInfo spectraFileInfoToUse))
                        {
                            // skip PSMs for files with no spectrum data input
                            continue;
                        }

                        // construct id
                        var ident = new Identification(spectraFileInfoToUse, baseSequence, modSequence, monoisotopicMass, ms2RetentionTime, chargeState, proteinGroups);
                        ids.Add(ident);
                    }
                }
                catch (Exception e)
                {
                    if (!silent)
                    {
                        Console.WriteLine("Problem reading line " + lineNum + " of the identification file" + "; " + e.Message);
                    }
                    return new List<Identification>();
                }
            }

            reader.Close();

            if (!silent)
            {
                Console.WriteLine("Done reading PSMs; found " + ids.Count);
            }

            return ids;
        }

        private static PsmFileType GetFileTypeFromHeader(string header)
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
                _qValueCol = Array.IndexOf(split, "QValue".ToLowerInvariant());
                _qValueNotchCol = Array.IndexOf(split, "QValue Notch".ToLowerInvariant());
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