# About
FlashLFQ is an ultrafast label-free quantification algorithm for mass-spectrometry proteomics. 

# Requirements
Input is a tab-separated value (TSV) text file of MS/MS identifications, in addition to one or more raw data files. Currently, .mzML and .raw files are supported. [Thermo MSFileReader](https://thermo.flexnetoperations.com/control/thmo/search?query=MSFileReader+3.0+SP2) (v3.0 SP2 is recommended) is required to read Thermo .raw files. A 64-bit machine running Microsoft Windows is also required to run FlashLFQ.

# Download
To download the latest standalone version of FlashLFQ, go [here](https://github.com/smith-chem-wisc/FlashLFQ/releases/latest). Click the .zip file (e.g. FlashLFQ.zip) and extract the contents to a desired location on your computer. 

Alternatively, FlashLFQ is bundled into MetaMorpheus, which can be downloaded [here](https://github.com/smith-chem-wisc/MetaMorpheus). MetaMorpheus is a full-featured GUI proteomics software suite that features mass-calibration, PTM-discovery, search algorithms, and FlashLFQ built in.

# Usage
FlashLFQ is currently a command-line program, though it is also built into the MetaMorpheus GUI (see [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)).

To use the FlashLFQ standalone version, run the "FlashLFQExecutable" program with command-line arguments. At minimum, the --idt (the identification file) and either the --raw or --rep (the raw file(s)) must be specified.

Preferably, when specifying a filepath, use the absolute file path inside of quotes. Examples are listed below.

**Accepted command-line arguments:**

    --idt [string | identification file path (TSV format)]
    
    --raw [string | MS data file (.raw or .mzML)]
    
    --rep [string | repository containing MS data files]
    
    --ppm [double | monoisotopic ppm tolerance] (default = 10)
    
    --iso [double | isotopic distribution tolerance in ppm] (default = 5)
    
    --sil [boolean | silent mode; no console output] (default = false)
    
    --pau [boolean | pause at end of run] (default = true)
    
    --int [boolean | integrate chromatographic peak intensity instead of using 
	  the apex intensity] (default = false)
    
    --chg [boolean | use only precursor charge state; when set to false, FlashLFQ looks 
	  for all charge states detected in the MS/MS identification file for each peptide] (default = false)

**Command-Line Examples:**

*FlashLFQExecutable --idt "C:\MyFolder\msms.txt" --rep "C:\MyFolder" --ppm 5*

*FlashLFQExecutable --idt "C:\MyFolder\msms.txt" --raw "C:\MyFolder\MyRawFile.raw" --ppm 5 --iso 3 --sil true --pau false --int true --chg false*

**Tab-Delimited Identification Text File**

The first line of the text file should contain column headers identifying what each column is. Note that MetaMorpheus (.psmtsv), Morpheus, MaxQuant (msms.txt), and TDPortal tab-delimited column headers are supported  natively and such files can be read without modification. For search software that lists decoys and PSMs above 1% FDR (e.g., MetaMorpheus), you may want to remove these prior to FlashLFQ analysis. FlashLFQ will probably crash if ambiguous PSMs are passed into it (e.g., a PSM with more than 2 peptides listed in one line).

The following headers are required in the list of MS/MS identifications:

    File Name - File extensions should be tolerated, but no extension is tested more extensively 
				(e.g. use MyFile and not MyFile.mzML)
    
    Base Sequence - Should only contain amino acid sequences, or it will likely result in a crash
    
    Full Sequence - Modified sequence. Can contain any letters, but must be consistent between the same 
					peptidoform to get accurate results
    
    Peptide Monoisotopic Mass - Theoretical monoisotopic mass, including modification mass
    
    Scan Retention Time - MS/MS identification scan retention time
    
    Precursor Charge - Charge of the ion selected for MS/MS resulting in the identification
    
    Protein Accession - Protein accession(s) for the peptide; protein quantification is still preliminary

As of v.0.1.69, a sample MS/MS identification file, a sample .mzML, and a sample .bat are included with FlashLFQ. You may modify the .bat file (e.g., with Notepad) to point to the directory containing the .psmtsv and .mzML files and simply double-click the bat to run FlashLFQ.

# Output
FlashLFQ outputs several text files, described here. The .tsv files are convenient to view with Microsoft Excel.

*Log.txt* - Log of the FlashLFQ run. Includes timestamps and quantification time for each file, total analysis time, directories used, and settings.

*QuantifiedPeaks.tsv* - Each chromatographic peak is shown here, even peaks that were not quantifiable (peak intensity = 0). Details about each peak, such as number of PSMs mapped, start/apex/end retention times, ppm error, etc are contained in this file. A peptide can have multiple peaks over the course of a run (e.g., oxidized peptidoforms elute at different times, etc). Ambiguous peaks are displayed with a | (pipe) delimiter to indicate more than one peptide mapped to that peak. 

*QuantifiedBaseSequences.tsv* - Peptide intensities are summed here within a run (including differently-modified forms of the same amino acid sequence) and displayed in a convenient format for comparing across runs. The identification type (MS/MS or MBR) is also indicated. A peptide with more than 30% of its intensity coming from ambiguous peak(s) is considered not quantifiable and is given an intensity of -1.

*QuantifiedModifiedSequences.tsv* - Similar to QuantifiedBaseSequences, but instead of being summed by Base Sequence, peptide intensities are summed by modified sequence; this makes it convenient to compare modified peptidoform intensities across runs.

# Development Status
    To do: 

    - Retention time calibration/matching between runs (currently in an early state)
    - Improved peak picking
    - Intensity normalization (especially between technical replicates, biological replicates, and fractions)
    - Protein quantification
    
