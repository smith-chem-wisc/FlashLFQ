## Build Status (AppVeyor)
[![Build status](https://ci.appveyor.com/api/projects/status/5mue0eiapbb6gk0u?svg=true)](https://ci.appveyor.com/project/robertmillikin/flashlfq)

## Docker Image
[![](https://images.microbadger.com/badges/version/smithchemwisc/flashlfq.svg)](https://hub.docker.com/r/smithchemwisc/flashlfq/tags)

## About
FlashLFQ is an ultrafast label-free quantification algorithm for mass-spectrometry proteomics. 

This repository is for the stand-alone application of FlashLFQ. FlashLFQ is also bundled into [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus).

## Requirements

**Windows requirements:**
- [.NET Framework 4.7.1](https://dotnet.microsoft.com/download/dotnet-framework)

**Linux/OSX requirements:**
- [.NET Core 2.1](https://dotnet.microsoft.com/download)

## Download
To download FlashLFQ, go [here](https://github.com/smith-chem-wisc/FlashLFQ/releases/latest). Click the FlashLFQ.zip file and extract the contents to a desired location on your computer.

FlashLFQ is also available as a [Docker container](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Docker-Image).

Alternatively, FlashLFQ is bundled into MetaMorpheus, which can be downloaded [here](https://github.com/smith-chem-wisc/MetaMorpheus). MetaMorpheus is a full-featured GUI proteomics software suite that includes mass calibration, PTM discovery, search algorithms, and FlashLFQ.

## Input
- Tab-separated text file of [MS/MS identifications](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Identification-Input-Formats) (.tsv, .psmtsv., .txt)
- One or more bottom-up mass spectra files. Supported formats are .raw or .mzML file formats on Windows, or .mzML format on Linux/OSX. You can convert other formats to .mzML using [MSConvert](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Converting-spectral-data-files-with-MSConvert).

## Usage
FlashLFQ can be used as a [command-line program](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Using-the-Command-Line) or in a [graphical user interface (GUI)](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Using-the-Graphical-User-Interface-(GUI)). It is also built into the MetaMorpheus GUI (see [MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)).

## Tutorial
To get started using FlashLFQ, please try the [vignette](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Vignettes). This is a helpful tutorial that provides the necessary input (spectra files, etc.) for you to try FlashLFQ. If you want to learn more about how FlashLFQ works, please check out the [Wiki](https://github.com/smith-chem-wisc/FlashLFQ/wiki).

## Help
The [Wiki](https://github.com/smith-chem-wisc/FlashLFQ/wiki) contains a lot of useful information including [a description of FlashLFQ's settings](https://github.com/smith-chem-wisc/FlashLFQ/wiki/FlashLFQ's-Settings), how to [get started with our vignette](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Vignettes), and how to [interpret the results in the Bayesian quantification file](https://github.com/smith-chem-wisc/FlashLFQ/wiki/Interpreting-Results). Check it out!

There are two methods to contact us. You can create a new issue on the [GitHub Issues page](https://github.com/smith-chem-wisc/FlashLFQ/issues) if you are comfortable with your question being public. This helps other people, because it is likely that someone else has the same question. For private correspondance, please email mm_support at chem dot wisc dot edu.

## Development Status
    To do: 
    Intensity imputation for missing values
