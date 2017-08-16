# About
FlashLFQ is an ultrafast label-free quantification algorithm for mass-spectrometry proteomics. 

# Requirements
Input is a tab-separated value (TSV) text file of MS/MS identifications, in addition to one or more raw data files. Currently, .mzml and .raw files are supported. ThermoMSFileReader v3.10 is required to read .raw files. A 64-bit machine running Microsoft Windows is also required.

# Download
To download the latest standalone version of FlashLFQ, go [here](https://github.com/smith-chem-wisc/FlashLFQ/releases/latest). Click the .zip file (e.g. FlashLFQ.v0.1.*.zip) and extract the contents to a desired location on your computer. 

FlashLFQ is bundled into MetaMorpheus, which can be downloaded [here](https://github.com/smith-chem-wisc/MetaMorpheus). MetaMorpheus is a full-featured GUI proteomics software suite that features mass-calibration, PTM-discovery, search algorithms, and FlashLFQ label-free quantification built in.

# Usage
FlashLFQ is currently a command-line program, though it is also built into the MetaMorpheus GUI (see [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)).

To use the FlashLFQ standalone version, run the "FlashLFQExecutable" program with command-line arguments.


**Accepted arguments:**

*--idt [string|identification file path (TSV format)]*
*--raw [string|MS data file (.raw or .mzml)]*
*--rep [string|directory containing MS data files]*
*--ppm [double|ppm tolerance] (default = 10)*
*--iso [double|isotopic distribution tolerance in ppm] (default = 5)*
*--sil [bool|silent mode] (default = false)*
*--pau [bool|pause at end of run] (default = true)*
*--int [bool|integrate features] (default = false)*
*--chg [bool|use only precursor charge state] (default = false)*

**Examples:**

*FlashLFQExecutable --idt "C:\MyFolder\msms.txt" --rep "C:\MyFolder" --ppm 5*

*FlashLFQExecutable --idt "C:\MyFolder\msms.txt" --raw "C:\MyFolder\MyRawFile.raw" --ppm 5 --iso 3 --sil true --pau false --int true --chg false*

# Output
FlashLFQ outputs several text files, described here:
*Log.txt*
*QuantifiedPeaks.tsv*
*QuantifiedBaseSequences.tsv*
*QuantifiedModifiedSequences.tsv*

The .tsv files are convenient to view with Microsoft Excel.

# Development Status
To do: 
- Retention time calibration/matching between runs (currently in an early state)
- Improved peak picking
- Intensity normalization (especially between technical replicates, biological replicates, and fractions)
- Protein quantification