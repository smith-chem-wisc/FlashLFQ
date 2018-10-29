using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlashLFQ;

namespace CMD
{
    enum PsmFileType { MetaMorpheus, Morpheus, MaxQuant, PeptideShaker, TdPortal, Generic, Unknown }

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
        private static Dictionary<string, double> _modSequenceToMonoMass;

        public static List<Identification> ReadPsms(string filepath, bool silent, List<SpectraFileInfo> rawfiles)
        {
            Dictionary<string, ProteinGroup> allProteinGroups = new Dictionary<string, ProteinGroup>();
            _modSequenceToMonoMass = new Dictionary<string, double>();
            List<Identification> ids = new List<Identification>();
            PsmFileType fileType = PsmFileType.Unknown;
            string[] delim = new string[] { ";", ",", " or ", "\"", "|" };

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
                        if (fileType == PsmFileType.TdPortal)
                        {
                            modSequence = baseSequence + modSequence;
                        }

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
                        if (fileType == PsmFileType.TdPortal)
                        {
                            chargeState = 1;
                        }
                        else if (fileType == PsmFileType.PeptideShaker)
                        {
                            string charge = new String(param[_chargeStCol].Where(Char.IsDigit).ToArray());
                            chargeState = int.Parse(charge);
                        }
                        else
                        {
                            chargeState = (int)double.Parse(param[_chargeStCol]);
                        }

                        // protein groups
                        List<string> proteinGroupStrings = new List<string>();
                        if (fileType == PsmFileType.MetaMorpheus)
                        {
                            // MetaMorpheus - use all proteins listed
                            var g = param[_protNameCol].Split(delim, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Any())
                            {
                                foreach (var pg in g)
                                {
                                    proteinGroupStrings.Add(pg.Trim());
                                }
                            }
                        }
                        else if (fileType == PsmFileType.Morpheus)
                        {
                            // Morpheus - only one protein listed, use it
                            proteinGroupStrings.Add(param[_protNameCol].Trim());
                        }
                        else if (fileType == PsmFileType.MaxQuant)
                        {
                            // MaxQuant - use the first protein listed
                            var g = param[_protNameCol].Split(delim, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Any())
                            {
                                proteinGroupStrings.Add(g.First().Trim());
                            }
                        }
                        else if (fileType == PsmFileType.PeptideShaker)
                        {
                            // Peptide Shaker - use all proteins listed
                            var g = param[_protNameCol].Split(delim, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Any())
                            {
                                foreach (var pg in g)
                                {
                                    proteinGroupStrings.Add(pg.Trim());
                                }
                            }
                        }
                        else if (fileType == PsmFileType.TdPortal)
                        {
                            // TDPortal - use base sequence as protein group
                            proteinGroupStrings.Add(baseSequence);
                        }
                        else
                        {
                            proteinGroupStrings.Add(param[_protNameCol]);
                        }

                        List<ProteinGroup> proteinGroups = new List<ProteinGroup>();
                        foreach (var proteinGroupName in proteinGroupStrings)
                        {
                            if (allProteinGroups.TryGetValue(proteinGroupName, out ProteinGroup pg))
                            {
                                proteinGroups.Add(pg);
                            }
                            else
                            {
                                ProteinGroup newPg = new ProteinGroup(proteinGroupName, "", "");
                                allProteinGroups.Add(proteinGroupName, newPg);
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

                return PsmFileType.PeptideShaker;
            }

            // TDPortal Input
            else if (split.Contains("File Name")
                && split.Contains("Sequence")
                && split.Contains("Modifications")
                && split.Contains("Monoisotopic Mass")
                && split.Contains("RetentionTime")
                && split.Contains("Accession")
                && split.Contains("% Cleavages"))
            {
                _fileNameCol = Array.IndexOf(split, "File Name");
                _baseSequCol = Array.IndexOf(split, "Sequence");
                _fullSequCol = Array.IndexOf(split, "Modifications");
                _monoMassCol = Array.IndexOf(split, "Monoisotopic Mass");
                _msmsRetnCol = Array.IndexOf(split, "RetentionTime");
                _protNameCol = Array.IndexOf(split, "Accession");

                return PsmFileType.TdPortal;
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

                return PsmFileType.Generic;
            }

            return type;
        }
    }
}
