using FlashLFQ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Util
{
    enum PsmFileType { MetaMorpheus, Morpheus, MaxQuant, PeptideShaker, Generic, Unknown }

    public class PsmReader
    {
        private static int _fileNameCol;
        private static int _baseSequCol;
        private static int _fullSequCol;
        private static int _monoMassCol;
        private static int _msmsRetnCol;
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

        private static readonly Dictionary<PsmFileType, string[]> delimiters = new Dictionary<PsmFileType, string[]>
        {
            { PsmFileType.MetaMorpheus, new string[] { "|", " or " } },
            { PsmFileType.Morpheus, new string[] { ";" } },
            { PsmFileType.MaxQuant, new string[] { ";" } },
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

            int lineNum = 1;

            while (reader.Peek() > 0)
            {
                string line = reader.ReadLine();

                try
                {
                    if (lineNum != 1)
                    {
                        if (fileType == PsmFileType.Unknown)
                        {
                            break;
                        }

                        var param = line.Split('\t');

                        // only quantify PSMs below 1% FDR
                        if (fileType == PsmFileType.MetaMorpheus && double.Parse(param[_qValueCol]) > 0.01)
                        {
                            break;
                        }
                        else if (fileType == PsmFileType.Morpheus && double.Parse(param[_qValueCol]) > 1.00)
                        {
                            break;
                        }

                        // only quantify PSMs below 1% notch FDR
                        if (fileType == PsmFileType.MetaMorpheus && double.Parse(param[_qValueNotchCol]) > 0.01)
                        {
                            continue;
                        }

                        // skip decoys
                        if ((fileType == PsmFileType.MetaMorpheus || fileType == PsmFileType.Morpheus) &&
                            param[_decoyCol].Contains("D"))
                        {
                            continue;
                        }

                        // spectrum file name
                        string fileName = param[_fileNameCol];

                        // base sequence
                        string baseSequence = param[_baseSequCol];
                        // skip ambiguous sequence in MetaMorpheus output
                        if (fileType == PsmFileType.MetaMorpheus && (baseSequence.Contains(" or ") || baseSequence.Contains("|")))
                        {
                            lineNum++;
                            continue;
                        }

                        // modified sequence
                        string modSequence = param[_fullSequCol];

                        // skip ambiguous sequence in MetaMorpheus output
                        if (fileType == PsmFileType.MetaMorpheus && (modSequence.Contains(" or ") || modSequence.Contains("|") || modSequence.Contains("too long")))
                        {
                            lineNum++;
                            continue;
                        }

                        // monoisotopic mass
                        double monoisotopicMass = double.Parse(param[_monoMassCol]);

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

                                lineNum++;
                                continue;
                            }
                        }
                        else
                        {
                            _modSequenceToMonoMass.Add(modSequence, monoisotopicMass);
                        }

                        // retention time
                        double ms2RetentionTime = double.Parse(param[_msmsRetnCol]);
                        if (fileType == PsmFileType.PeptideShaker)
                        {
                            // peptide shaker RT is in seconds - convert to minutes
                            ms2RetentionTime = ms2RetentionTime / 60.0;
                        }

                        if (ms2RetentionTime < 0)
                        {
                            if (!silent)
                            {
                                Console.WriteLine("Caution! PSM with sequence " + modSequence + " at line " +
                                   lineNum + " could not be read; retention time was negative");
                            }

                            lineNum++;
                            continue;
                        }

                        // charge state
                        int chargeState;
                        if (fileType == PsmFileType.PeptideShaker)
                        {
                            string charge = new String(param[_chargeStCol].Where(Char.IsDigit).ToArray());
                            chargeState = int.Parse(charge);
                        }
                        else
                        {
                            chargeState = (int)double.Parse(param[_chargeStCol]);
                        }

                        // protein groups
                        // use all proteins listed
                        List<ProteinGroup> proteinGroups = new List<ProteinGroup>();
                        var proteins = param[_protNameCol].Split(delimiters[fileType], StringSplitOptions.None);

                        string[] genes = null;
                        if (_geneNameCol >= 0)
                        {
                            genes = param[_geneNameCol].Split(delimiters[fileType], StringSplitOptions.None);
                        }

                        string[] organisms = null;
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

                        // construct id
                        var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
                        var rawFileInfoToUse = rawfiles.FirstOrDefault(p => p.FilenameWithoutExtension.Equals(fileNameNoExt));
                        if (rawFileInfoToUse == null)
                        {
                            // skip PSMs for files with no spectrum data input
                            lineNum++;
                            continue;
                        }

                        var ident = new Identification(rawFileInfoToUse, baseSequence, modSequence, monoisotopicMass, ms2RetentionTime, chargeState, proteinGroups);
                        ids.Add(ident);
                    }
                    else
                    {
                        fileType = GetFileTypeFromHeader(line);
                    }

                    lineNum++;
                }
                catch (Exception)
                {
                    if (!silent)
                    {
                        Console.WriteLine("Problem reading line " + lineNum + " of the identification file");
                    }
                    return new List<Identification>();
                }
            }

            if (fileType == PsmFileType.Unknown)
            {
                throw new Exception("Could not interpret PSM header labels from file: " + filepath);
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

            var split = header.Split('\t');

            // MetaMorpheus MS/MS input
            if (split.Contains("File Name")
                        && split.Contains("Base Sequence")
                        && split.Contains("Full Sequence")
                        && split.Contains("Peptide Monoisotopic Mass")
                        && split.Contains("Scan Retention Time")
                        && split.Contains("Precursor Charge")
                        && split.Contains("Protein Accession")
                        && split.Contains("Decoy/Contaminant/Target")
                        && split.Contains("QValue")
                        && split.Contains("QValue Notch"))
            {
                _fileNameCol = Array.IndexOf(split, "File Name");
                _baseSequCol = Array.IndexOf(split, "Base Sequence");
                _fullSequCol = Array.IndexOf(split, "Full Sequence");
                _monoMassCol = Array.IndexOf(split, "Peptide Monoisotopic Mass");
                _msmsRetnCol = Array.IndexOf(split, "Scan Retention Time");
                _chargeStCol = Array.IndexOf(split, "Precursor Charge");
                _protNameCol = Array.IndexOf(split, "Protein Accession");
                _decoyCol = Array.IndexOf(split, "Decoy/Contaminant/Target");
                _qValueCol = Array.IndexOf(split, "QValue");
                _qValueNotchCol = Array.IndexOf(split, "QValue Notch");
                _geneNameCol = Array.IndexOf(split, "Gene Name");
                _organismCol = Array.IndexOf(split, "Organism Name");

                return PsmFileType.MetaMorpheus;
            }

            // Morpheus MS/MS input
            else if (split.Contains("Filename")
                && split.Contains("Base Peptide Sequence")
                && split.Contains("Peptide Sequence")
                && split.Contains("Theoretical Mass (Da)")
                && split.Contains("Retention Time (minutes)")
                && split.Contains("Precursor Charge")
                && split.Contains("Protein Description")
                && split.Contains("Decoy?")
                && split.Contains("Q-Value (%)"))
            {
                _fileNameCol = Array.IndexOf(split, "Filename");
                _baseSequCol = Array.IndexOf(split, "Base Peptide Sequence");
                _fullSequCol = Array.IndexOf(split, "Peptide Sequence");
                _monoMassCol = Array.IndexOf(split, "Theoretical Mass (Da)");
                _msmsRetnCol = Array.IndexOf(split, "Retention Time (minutes)");
                _chargeStCol = Array.IndexOf(split, "Precursor Charge");
                _protNameCol = Array.IndexOf(split, "Protein Description");
                _decoyCol = Array.IndexOf(split, "Decoy?");
                _qValueCol = Array.IndexOf(split, "Q-Value (%)");

                _geneNameCol = Array.IndexOf(split, "Gene Name"); // probably doesn't exist
                _organismCol = Array.IndexOf(split, "Organism Name");

                return PsmFileType.Morpheus;
            }

            // MaxQuant MS/MS input
            else if (split.Contains("Raw file")
                && split.Contains("Sequence")
                && split.Contains("Modified sequence")
                && split.Contains("Mass")
                && split.Contains("Retention time")
                && split.Contains("Charge")
                && split.Contains("Proteins"))
            {
                _fileNameCol = Array.IndexOf(split, "Raw file");
                _baseSequCol = Array.IndexOf(split, "Sequence");
                _fullSequCol = Array.IndexOf(split, "Modified sequence");
                _monoMassCol = Array.IndexOf(split, "Mass");
                _msmsRetnCol = Array.IndexOf(split, "Retention time");
                _chargeStCol = Array.IndexOf(split, "Charge");
                _protNameCol = Array.IndexOf(split, "Proteins");
                _geneNameCol = Array.IndexOf(split, "Gene Names");

                _organismCol = Array.IndexOf(split, "Organism Name");

                return PsmFileType.MaxQuant;
            }

            // Peptide Shaker Input
            else if (split.Contains("Spectrum File")
                && split.Contains("Sequence")
                && split.Contains("Modified Sequence")
                && split.Contains("Theoretical Mass")
                && split.Contains("RT")
                && split.Contains("Identification Charge")
                && split.Contains("Protein(s)"))
            {
                _fileNameCol = Array.IndexOf(split, "Spectrum File");
                _baseSequCol = Array.IndexOf(split, "Sequence");
                _fullSequCol = Array.IndexOf(split, "Modified Sequence");
                _monoMassCol = Array.IndexOf(split, "Theoretical Mass");
                _msmsRetnCol = Array.IndexOf(split, "RT");
                _chargeStCol = Array.IndexOf(split, "Identification Charge");
                _protNameCol = Array.IndexOf(split, "Protein(s)");

                _geneNameCol = Array.IndexOf(split, "Gene Name"); // probably doesn't exist
                _organismCol = Array.IndexOf(split, "Organism Name");

                return PsmFileType.PeptideShaker;
            }

            // Generic MS/MS input
            if (split.Contains("File Name")
                        && split.Contains("Base Sequence")
                        && split.Contains("Full Sequence")
                        && split.Contains("Peptide Monoisotopic Mass")
                        && split.Contains("Scan Retention Time")
                        && split.Contains("Precursor Charge")
                        && split.Contains("Protein Accession"))
            {
                _fileNameCol = Array.IndexOf(split, "File Name");
                _baseSequCol = Array.IndexOf(split, "Base Sequence");
                _fullSequCol = Array.IndexOf(split, "Full Sequence");
                _monoMassCol = Array.IndexOf(split, "Peptide Monoisotopic Mass");
                _msmsRetnCol = Array.IndexOf(split, "Scan Retention Time");
                _chargeStCol = Array.IndexOf(split, "Precursor Charge");
                _protNameCol = Array.IndexOf(split, "Protein Accession");

                _geneNameCol = Array.IndexOf(split, "Gene Name"); // probably doesn't exist
                _organismCol = Array.IndexOf(split, "Organism Name");

                return PsmFileType.Generic;
            }

            return type;
        }
    }
}
