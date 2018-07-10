using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlashLFQ;

namespace CMD
{
    enum PsmFileType { MetaMorpheus, Morpheus, MaxQuant, PeptideShaker, TDPortal, Generic, Unknown }

    public class PsmReader
    {
        private static int fileNameCol;
        private static int baseSequCol;
        private static int fullSequCol;
        private static int monoMassCol;
        private static int msmsRetnCol;
        private static int chargeStCol;
        private static int protNameCol;
        private static int decoyCol;
        private static int qValueCol;
        private static Dictionary<string, double> modSequenceToMonoMass;

        public static List<Identification> ReadPsms(string filepath, bool silent, List<SpectraFileInfo> rawfiles)
        {
            Dictionary<string, ProteinGroup> allProteinGroups = new Dictionary<string, ProteinGroup>();
            modSequenceToMonoMass = new Dictionary<string, double>();
            List<Identification> ids = new List<Identification>();
            PsmFileType fileType = PsmFileType.Unknown;
            string[] delim = new string[] { ";", ",", " or ", "\"" };

            if (!silent)
                Console.WriteLine("Opening PSM file " + filepath);

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filepath);
            }
            catch (Exception e)
            {
                if (!silent)
                    Console.WriteLine("Error reading file " + filepath + "\n" + e.Message);
                return new List<Identification>();
            }
            string line;
            int lineNum = 1;

            while (reader.Peek() > 0)
            {
                line = reader.ReadLine();

                try
                {
                    if (lineNum != 1)
                    {
                        if (fileType == PsmFileType.Unknown)
                            break;

                        var param = line.Split('\t');

                        // only quantify PSMs below 1% FDR
                        if (fileType == PsmFileType.MetaMorpheus && double.Parse(param[qValueCol]) > 0.01)
                            break;
                        else if (fileType == PsmFileType.Morpheus && double.Parse(param[qValueCol]) > 1.00)
                            break;

                        // spectrum file name
                        string fileName = param[fileNameCol];

                        // base sequence
                        string BaseSequence = param[baseSequCol];
                        // skip ambiguous sequence in MetaMorpheus output
                        if (fileType == PsmFileType.MetaMorpheus && (BaseSequence.Contains(" or ") || BaseSequence.Contains("|")))
                        {
                            lineNum++;
                            continue;
                        }

                        // modified sequence
                        string ModSequence = param[fullSequCol];
                        if (fileType == PsmFileType.TDPortal)
                            ModSequence = BaseSequence + ModSequence;
                        // skip ambiguous sequence in MetaMorpheus output
                        if (fileType == PsmFileType.MetaMorpheus && (ModSequence.Contains(" or ") || ModSequence.Contains("|")))
                        {
                            lineNum++;
                            continue;
                        }

                        // monoisotopic mass
                        double monoisotopicMass = double.Parse(param[monoMassCol]);

                        if (modSequenceToMonoMass.TryGetValue(ModSequence, out double storedMonoisotopicMass))
                        {
                            if (storedMonoisotopicMass != monoisotopicMass)
                            {
                                if (!silent)
                                    Console.WriteLine("Caution! PSM with sequence " + ModSequence + " at line " + lineNum + " could not be read; " +
                                        "a peptide with the same modified sequence but a different monoisotopic mass has already been added");
                                lineNum++;
                                continue;
                            }
                        }
                        else
                        {
                            modSequenceToMonoMass.Add(ModSequence, monoisotopicMass);
                        }

                        // retention time
                        double ms2RetentionTime = double.Parse(param[msmsRetnCol]);
                        if (fileType == PsmFileType.PeptideShaker)
                            ms2RetentionTime = ms2RetentionTime / 60.0; // peptide shaker RT is in seconds - convert to minutes

                        // charge state
                        int chargeState;
                        if (fileType == PsmFileType.TDPortal)
                            chargeState = 1;
                        else if (fileType == PsmFileType.PeptideShaker)
                        {
                            string charge = new String(param[chargeStCol].Where(Char.IsDigit).ToArray());
                            chargeState = int.Parse(charge);
                        }
                        else
                            chargeState = (int)double.Parse(param[chargeStCol]);

                        // protein groups
                        List<string> proteinGroupStrings = new List<string>();
                        if (fileType == PsmFileType.MetaMorpheus)
                        {
                            // MetaMorpheus - use all proteins listed
                            var g = param[protNameCol].Split(delim, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Any())
                                foreach (var pg in g)
                                    proteinGroupStrings.Add(pg.Trim());
                        }
                        else if (fileType == PsmFileType.Morpheus)
                        {
                            // Morpheus - only one protein listed, use it
                            proteinGroupStrings.Add(param[protNameCol].Trim());
                        }
                        else if (fileType == PsmFileType.MaxQuant)
                        {
                            // MaxQuant - use the first protein listed
                            var g = param[protNameCol].Split(delim, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Any())
                                proteinGroupStrings.Add(g.First().Trim());
                        }
                        else if (fileType == PsmFileType.PeptideShaker)
                        {
                            // Peptide Shaker - use all proteins listed
                            var g = param[protNameCol].Split(delim, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Any())
                                foreach (var pg in g)
                                    proteinGroupStrings.Add(pg.Trim());
                        }
                        else if (fileType == PsmFileType.TDPortal)
                        {
                            // TDPortal - use base sequence as protein group
                            proteinGroupStrings.Add(BaseSequence);
                        }
                        else
                        {
                            proteinGroupStrings.Add(param[protNameCol]);
                        }

                        List<ProteinGroup> proteinGroups = new List<ProteinGroup>();
                        foreach(var proteinGroupName in proteinGroupStrings)
                        {
                            if(allProteinGroups.TryGetValue(proteinGroupName, out ProteinGroup pg))
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
                        var rawFileInfoToUse = rawfiles.Where(p => p.filenameWithoutExtension.Equals(fileNameNoExt)).FirstOrDefault();
                        if (rawFileInfoToUse == null)
                        {
                            // skip PSMs for files with no spectrum data input
                            lineNum++;
                            continue;
                        }

                        var ident = new Identification(rawFileInfoToUse, BaseSequence, ModSequence, monoisotopicMass, ms2RetentionTime, chargeState, proteinGroups);
                        ids.Add(ident);
                    }
                    else
                    {
                        fileType = GetFileTypeFromHeader(line);
                    }

                    lineNum++;
                }
                catch (Exception e)
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
                Console.WriteLine("Done reading PSMs");
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
                        && split.Contains("QValue"))
            {
                fileNameCol = Array.IndexOf(split, "File Name");
                baseSequCol = Array.IndexOf(split, "Base Sequence");
                fullSequCol = Array.IndexOf(split, "Full Sequence");
                monoMassCol = Array.IndexOf(split, "Peptide Monoisotopic Mass");
                msmsRetnCol = Array.IndexOf(split, "Scan Retention Time");
                chargeStCol = Array.IndexOf(split, "Precursor Charge");
                protNameCol = Array.IndexOf(split, "Protein Accession");
                decoyCol = Array.IndexOf(split, "Decoy/Contaminant/Target");
                qValueCol = Array.IndexOf(split, "QValue");

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
                fileNameCol = Array.IndexOf(split, "Filename");
                baseSequCol = Array.IndexOf(split, "Base Peptide Sequence");
                fullSequCol = Array.IndexOf(split, "Peptide Sequence");
                monoMassCol = Array.IndexOf(split, "Theoretical Mass (Da)");
                msmsRetnCol = Array.IndexOf(split, "Retention Time (minutes)");
                chargeStCol = Array.IndexOf(split, "Precursor Charge");
                protNameCol = Array.IndexOf(split, "Protein Description");
                decoyCol = Array.IndexOf(split, "Decoy?");
                qValueCol = Array.IndexOf(split, "Q-Value (%)");

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
                fileNameCol = Array.IndexOf(split, "Raw file");
                baseSequCol = Array.IndexOf(split, "Sequence");
                fullSequCol = Array.IndexOf(split, "Modified sequence");
                monoMassCol = Array.IndexOf(split, "Mass");
                msmsRetnCol = Array.IndexOf(split, "Retention time");
                chargeStCol = Array.IndexOf(split, "Charge");
                protNameCol = Array.IndexOf(split, "Proteins");

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
                fileNameCol = Array.IndexOf(split, "Spectrum File");
                baseSequCol = Array.IndexOf(split, "Sequence");
                fullSequCol = Array.IndexOf(split, "Modified Sequence");
                monoMassCol = Array.IndexOf(split, "Theoretical Mass");
                msmsRetnCol = Array.IndexOf(split, "RT");
                chargeStCol = Array.IndexOf(split, "Identification Charge");
                protNameCol = Array.IndexOf(split, "Protein(s)");

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
                fileNameCol = Array.IndexOf(split, "File Name");
                baseSequCol = Array.IndexOf(split, "Sequence");
                fullSequCol = Array.IndexOf(split, "Modifications");
                monoMassCol = Array.IndexOf(split, "Monoisotopic Mass");
                msmsRetnCol = Array.IndexOf(split, "RetentionTime");
                protNameCol = Array.IndexOf(split, "Accession");

                return PsmFileType.TDPortal;
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
                fileNameCol = Array.IndexOf(split, "File Name");
                baseSequCol = Array.IndexOf(split, "Base Sequence");
                fullSequCol = Array.IndexOf(split, "Full Sequence");
                monoMassCol = Array.IndexOf(split, "Peptide Monoisotopic Mass");
                msmsRetnCol = Array.IndexOf(split, "Scan Retention Time");
                chargeStCol = Array.IndexOf(split, "Precursor Charge");
                protNameCol = Array.IndexOf(split, "Protein Accession");

                return PsmFileType.Generic;
            }

            return type;
        }
    }
}
