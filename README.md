[![Build status](https://ci.appveyor.com/api/projects/status/5mue0eiapbb6gk0u?svg=true)](https://ci.appveyor.com/project/robertmillikin/flashlfq)

## About
FlashLFQ is an ultrafast label-free quantification algorithm for mass-spectrometry proteomics. 

This repository is for the stand-alone application of FlashLFQ. FlashLFQ is also bundled into [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus).

## Requirements
The graphical user interface (GUI) version is Windows-only. The .NET framework version of the command-line is also Windows only. Both require [.NET Framework 4.7.1](https://dotnet.microsoft.com/download/dotnet-framework). The command-line .NET Core version can be used on Windows, Linux, or OSX, and requires [.NET Core](https://dotnet.microsoft.com/download) to be installed.

The input to FlashLFQ is a tab-separated value (TSV) text file of [MS/MS identifications](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Identification-Input-Formats) and one or more bottom-up mass spectra files. Currently, .mzML and .raw file formats are supported. You can convert other formats to .mzML using [MSConvert](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Converting-spectral-data-files-with-MSConvert).

Older versions of FlashLFQ (before 1.0.0) required [MSFileReader](https://thermo.flexnetoperations.com/control/thmo/search?query=MSFileReader+3.0+SP2) to read Thermo .raw files. MSFileReader is no longer required unless you are using an old version of FlashLFQ.

## Download
To download the latest standalone version of FlashLFQ, go [here](https://github.com/smith-chem-wisc/FlashLFQ/releases/latest). Click the FlashLFQ.zip file and extract the contents to a desired location on your computer.

Alternatively, FlashLFQ is bundled into MetaMorpheus, which can be downloaded [here](https://github.com/smith-chem-wisc/MetaMorpheus). MetaMorpheus is a full-featured GUI proteomics software suite that features mass-calibration, PTM-discovery, search algorithms, and FlashLFQ built in.

## Usage
FlashLFQ can be used as a [command-line program](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Using-the-Command-Line) or in a [graphical user interface (GUI)](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Using-the-Graphical-User-Interface-(GUI)). It is also built into the MetaMorpheus GUI (see [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)).

## Tutorial Vignette
To get started using FlashLFQ, you may want to walk through the [vignette](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Vignettes). If you want to learn more about how FlashLFQ works, you can browse the [Wiki](https://github.com/smith-chem-wisc/FlashLFQ/wiki). 

## Development Status
    To do: 
    Intensity imputation for missing values
    
