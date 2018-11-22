[![Build status](https://ci.appveyor.com/api/projects/status/5mue0eiapbb6gk0u?svg=true)](https://ci.appveyor.com/project/robertmillikin/flashlfq)

# About
FlashLFQ is an ultrafast label-free quantification algorithm for mass-spectrometry proteomics. 

# Requirements
Input is a tab-separated value (TSV) text file of MS/MS identifications, in addition to one or more bottom-up mass spectra files. Currently, .mzML and .raw file formats are supported. [Thermo MSFileReader](https://thermo.flexnetoperations.com/control/thmo/search?query=MSFileReader+3.0+SP2) is required to read Thermo .raw files. The version of MSFileReader that we recommend installing is v3.0 SP2. A 64-bit machine running Microsoft Windows is also required to run the standalone version of FlashLFQ.

# Download
To download the latest standalone version of FlashLFQ, go [here](https://github.com/smith-chem-wisc/FlashLFQ/releases/latest). Click the FlashLFQ.zip file and extract the contents to a desired location on your computer. Run either the GUI.exe for the graphical interface, or the CMD.exe for the command-line version.

Alternatively, FlashLFQ is bundled into MetaMorpheus, which can be downloaded [here](https://github.com/smith-chem-wisc/MetaMorpheus). MetaMorpheus is a full-featured GUI proteomics software suite that features mass-calibration, PTM-discovery, search algorithms, and FlashLFQ built in.

# Usage
FlashLFQ can be used as a command-line program or in a graphical user interface (GUI). It is also built into the MetaMorpheus GUI (see [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)).

To use the GUI version, simply drag and drop identification file(s) and spectra file(s), edit your settings in the menu in the top left, define your experimental design if you are normalizing, and then click "Run FlashLFQ". See **Graphical User Interface (GUI)** section.

To use the FlashLFQ standalone command-line version, run the "CMD.exe" program with command-line arguments. At minimum, the --idt (the identification file) and --rep (the spectra file repository) must be specified.

If you want to normalize results using the command-line, you must create an experimental design TSV file and place it in the directory with your spectra files.

Preferably, when specifying a filepath, use the absolute file path inside of quotes. Examples are listed below.
NOTE: On Linux, absolute file paths do not currently work. See Issue [#71](https://github.com/smith-chem-wisc/FlashLFQ/issues/71).

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

    --nor [boolean | normalize intensity results; experimental design needs to be defined to do this] 
	  (default = false)

    --pro [boolean | advanced protein quantification; can take a long time if you're on a single-threaded machine] 
	  (default = false)

**Command-Line Example:**

*CMD.exe --idt "C:\MyFolder\msms.txt" --rep "C:\MyFolder" --ppm 5 --chg false*

**Graphical User Interface (GUI):**

The GUI can be used by opening GUI.exe. The identification files and spectra files can be added by drag-and-drop or by clicking the "Add" button in the respective area. Settings can be changed by going to Settings -> Open FlashLFQ Settings. The Experimental Design (required for normalization) is specified by clicking the "Experimental Design" button under the mass spectra files area.

**Tab-Delimited Identification Input Text File:**

The first line of the text file should contain column headers identifying what each column is. Note that MetaMorpheus (.psmtsv), Morpheus, Peptide Shaker (.tab), and MaxQuant (msms.txt) tab-delimited column headers are supported natively and such files can be read without editing. For search software that lists decoys and PSMs above 1% FDR, you may want to remove these prior to FlashLFQ analysis. FlashLFQ will probably crash if ambiguous PSMs are passed into it (e.g., a PSM with more than 2 peptides listed in one line).

The following headers are required in the list of MS/MS identifications:

    File Name - With or without file extension (e.g. MyFile or MyFile.mzML)
    
    Base Sequence - Should only contain an amino acid sequence (e.g., PEPTIDE and not PEPT[Phosphorylation]IDE
    
    Full Sequence - Modified sequence. Can contain any characters (e.g., PEPT[Phosphorylation]IDE is fine), but must be consistent between the same peptidoform to get accurate results
    
    Peptide Monoisotopic Mass - Theoretical monoisotopic mass, including modification mass
    
    Scan Retention Time - MS/MS identification scan retention time
    
    Precursor Charge - Charge of the ion selected for MS/MS resulting in the identification
    
    Protein Accession - Protein accession(s) for the peptide

# Output
FlashLFQ outputs several text files, described here. The .tsv files are convenient to view with Microsoft Excel.

*QuantifiedPeaks.tsv* - Each chromatographic peak is shown here, even peaks that were not quantifiable (peak intensity = 0). Details about each peak, such as number of PSMs mapped, start/apex/end retention times, ppm error, etc are contained in this file. A peptide can have multiple peaks over the course of a run (e.g., oxidized peptidoforms elute at different times, etc). Ambiguous peaks are displayed with a | (pipe) delimiter to indicate more than one peptide mapped to that peak. 

*QuantifiedPeptides.tsv* - Peptide intensities are summed by modified sequence; this makes it convenient to compare modified peptidoform intensities across runs.

*QuantifiedProteins.tsv* - Lists protein accession and in the future will include gene and organism if the TSV contains it. The intensity is either a) the sum of the 3 most intense peptides or b) (Advanced protein quant) a weighted-average of the intensities of the peptides assigned to the protein. The weights are determined by how well the peptide co-varies with the other peptides assigned to that protein. See [Diffacto](http://www.mcponline.org/content/16/5/936.full).

# Example Experimental Design File
Note: You do not need to download this if you're using the graphical version of FlashLFQ. You only need to download this and edit it manually if you're running the command-line version.
https://github.com/smith-chem-wisc/MetaMorpheus/files/2048804/ExperimentalDesign.zip

# Development Status
    To do: 

    - Improved retention time calibration/matching between runs (currently in an early state)
    
