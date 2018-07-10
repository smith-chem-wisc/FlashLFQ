[![Build status](https://ci.appveyor.com/api/projects/status/5mue0eiapbb6gk0u?svg=true)](https://ci.appveyor.com/project/robertmillikin/flashlfq)

# About
FlashLFQ is an ultrafast label-free quantification algorithm for mass-spectrometry proteomics. 

# Requirements
Input is a tab-separated value (TSV) text file of MS/MS identifications, in addition to one or more raw data files. Currently, .mzML and .raw files are supported. [Thermo MSFileReader](https://thermo.flexnetoperations.com/control/thmo/search?query=MSFileReader+3.0+SP2) is required to read Thermo .raw files. The version of MSFileReader that we recommend installing is v3.0 SP2. A 64-bit machine running Microsoft Windows is also required to run the standalone version of FlashLFQ.

# Download
To download the latest standalone version of FlashLFQ, go [here](https://github.com/smith-chem-wisc/FlashLFQ/releases/latest). Click the FlashLFQ.zip file and extract the contents to a desired location on your computer. 

Alternatively, FlashLFQ is bundled into MetaMorpheus, which can be downloaded [here](https://github.com/smith-chem-wisc/MetaMorpheus). MetaMorpheus is a full-featured GUI proteomics software suite that features mass-calibration, PTM-discovery, search algorithms, and FlashLFQ built in.

# Usage
FlashLFQ can be used as a command-line program or in a graphical user interface (GUI). It is also built into the MetaMorpheus GUI (see [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)).

To use the FlashLFQ standalone command-line version, run the "CMD.exe" program with command-line arguments. At minimum, the --idt (the identification file) and --rep (the spectra file repository) must be specified.

Preferably, when specifying a filepath, use the absolute file path inside of quotes. Examples are listed below.

**Accepted command-line arguments:**

    --idt [string | identification file path (TSV format); REQUIRED]
   
    --rep [string | repository containing MS data files; REQUIRED]
    
    --out [string | directory to output files to (default = identification folder)]
    
    --ppm [double | monoisotopic ppm tolerance] (default = 10)
    
    --iso [double | isotopic distribution tolerance in ppm] (default = 5)
    
    --sil [boolean | silent mode; no console output] (default = false)
    
    --int [boolean | integrate chromatographic peak intensity instead of using 
	  the apex intensity] (default = false)
    
    --chg [boolean | use only precursor charge state; when set to false, FlashLFQ looks 
	  for all charge states detected in the MS/MS identification file for each peptide] (default = false)

**Command-Line Example:**

*FlashLFQExecutable --idt "C:\MyFolder\msms.txt" --rep "C:\MyFolder" --ppm 5 --chg false*

**Graphical User Interface (GUI)**
The GUI can be used by opening GUI.exe. The identification files and spectra files can be added by drag-and-drop or by clicking the "Add" button in the respective area. Settings can be changed by going to Settings -> Open FlashLFQ Settings. The Experimental Design (required for normalization) is specified by clicking the "Experimental Design" button under the mass spectra files area.

**Tab-Delimited Identification Input Text File**

The first line of the text file should contain column headers identifying what each column is. Note that MetaMorpheus (.psmtsv), Morpheus, Peptide Shaker (.tab), and MaxQuant (msms.txt) tab-delimited column headers are supported natively and such files can be read without editing. For search software that lists decoys and PSMs above 1% FDR, you may want to remove these prior to FlashLFQ analysis. FlashLFQ will probably crash if ambiguous PSMs are passed into it (e.g., a PSM with more than 2 peptides listed in one line).

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

# Output
FlashLFQ outputs several text files, described here. The .tsv files are convenient to view with Microsoft Excel.

*QuantifiedPeaks.tsv* - Each chromatographic peak is shown here, even peaks that were not quantifiable (peak intensity = 0). Details about each peak, such as number of PSMs mapped, start/apex/end retention times, ppm error, etc are contained in this file. A peptide can have multiple peaks over the course of a run (e.g., oxidized peptidoforms elute at different times, etc). Ambiguous peaks are displayed with a | (pipe) delimiter to indicate more than one peptide mapped to that peak. 

*QuantifiedBaseSequences.tsv* - Peptide intensities are summed here within a run (including differently-modified forms of the same amino acid sequence) and displayed in a convenient format for comparing across runs. The identification type (MS/MS or MBR) is also indicated. A peptide with more than 30% of its intensity coming from ambiguous peak(s) is considered not quantifiable and is given an intensity of -1.

*QuantifiedModifiedSequences.tsv* - Similar to QuantifiedBaseSequences, but instead of being summed by Base Sequence, peptide intensities are summed by modified sequence; this makes it convenient to compare modified peptidoform intensities across runs.

# Development Status
    To do: 

    - Improved retention time calibration/matching between runs (currently in an early state)
    - Improved protein quantification
    
