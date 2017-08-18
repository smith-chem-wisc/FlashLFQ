# About
FlashLFQ is an ultrafast label-free quantification algorithm for mass-spectrometry proteomics. 

# Requirements
Input is a tab-separated value (TSV) text file of MS/MS identifications, in addition to one or more raw data files. Currently, .mzml and .raw files are supported. ThermoMSFileReader (v3.0 SP2 is recommended) is required to read .raw files. A 64-bit machine running Microsoft Windows is also required to run FlashLFQ.

# Download
To download the latest standalone version of FlashLFQ, go [here](https://github.com/smith-chem-wisc/FlashLFQ/releases/latest). Click the .zip file (e.g. FlashLFQ.v0.1.*.zip) and extract the contents to a desired location on your computer. 

Alternatively, FlashLFQ is bundled into MetaMorpheus, which can be downloaded [here](https://github.com/smith-chem-wisc/MetaMorpheus). MetaMorpheus is a full-featured GUI proteomics software suite that features mass-calibration, PTM-discovery, search algorithms, and FlashLFQ built in.

# Usage
FlashLFQ is currently a command-line program, though it is also built into the MetaMorpheus GUI (see [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)).

To use the FlashLFQ standalone version, run the "FlashLFQExecutable" program with command-line arguments. At minimum, the --idt and either the --raw or --rep must be specified.


**Accepted arguments:**

    --idt [string | identification file path (TSV format)]
    
    --raw [string | MS data file (.raw or .mzml)]
    
    --rep [string | repository containing MS data files]
    
    --ppm [double | monoisotopic ppm tolerance] (default = 10)
    
    --iso [double | isotopic distribution tolerance in ppm] (default = 5)
    
    --sil [boolean | silent mode; no console output] (default = false)
    
    --pau [boolean | pause at end of run] (default = true)
    
    --int [boolean | integrate chromatographic peak intensity instead of using 
	  the apex intensity] (default = false)
    
    --chg [boolean | use only precursor charge state; when set to false, FlashLFQ looks 
	  for all charge states detected in the MS/MS identification file for each peptide] (default = false)

**Examples:**

*FlashLFQExecutable --idt "C:\MyFolder\msms.txt" --rep "C:\MyFolder" --ppm 5*

*FlashLFQExecutable --idt "C:\MyFolder\msms.txt" --raw "C:\MyFolder\MyRawFile.raw" --ppm 5 --iso 3 --sil true --pau false --int true --chg false*

# Output
FlashLFQ outputs several text files, described here. The .tsv files are convenient to view with Microsoft Excel.

*Log.txt* - Log of the FlashLFQ run. Includes timestamps and quantification time for each file, total analysis time, directories used, and settings.

*QuantifiedPeaks.tsv* - Each chromatographic peak is shown here, even peaks that were not able to be quantified (peak intensity = 0). Details about each peak, such as number of PSMs mapped, start/apex/end retention times, ppm error, etc are contained in this file. A peptide can have multiple peaks over the course of a run (e.g., oxidized peptidoforms elute at different times, etc). Ambiguous peaks are displayed with a | (pipe) delimiter to indicate more than one peptide mapped to that peak. 

*QuantifiedBaseSequences.tsv* - Peptide intensities are summed here within a run (including differently-modified forms of the same amino acid sequence) and displayed in a convenient format for comparing across runs. The identification type (MS/MS or MBR) is also indicated. A peptide with more than 30% of its intensity coming from ambiguous peak(s) is considered not quantifiable and is given an intensity of -1 here.

*QuantifiedModifiedSequences.tsv* - Similar to QuantifiedBaseSequences, but instead of being summed by Base Sequence, peptide intensities are summed by modified sequence; this makes it convenient to compare modified peptidoforms across runs.

# Development Status
    To do: 

    - Retention time calibration/matching between runs (currently in an early state)
    - Improved peak picking
    - Intensity normalization (especially between technical replicates, biological replicates, and fractions)
    - Protein quantification
    