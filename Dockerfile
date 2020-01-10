## Base image is the Alpine Linux distro with .NET Core runtime
FROM mcr.microsoft.com/dotnet/core/runtime:2.1-alpine AS build

## Install dependencies and download/unzip FlashLFQ
## "unzip" is an de-archiving package
## "wget" is a package for downloading files
RUN apk update \
	&& apk add unzip \
	&& apk add wget \
	&& wget https://github.com/smith-chem-wisc/FlashLFQ/releases/latest/download/FlashLFQ_DotNetCore.zip \
	&& unzip FlashLFQ_DotNetCore.zip

## Set the entrypoint of the Docker image to CMD.dll
ENTRYPOINT ["dotnet", "CMD.dll"]

## Build example:
## docker build -t flashlfq .

## Run example (Note that spectra files must be in .mzML format (.raw is not supported yet on .NET Core)):
## docker run --rm -v C:/Data/Vignette_FlashLFQ:/mnt/data flashlfq --idt ./mnt/data/MSConvert/AllPSMs.psmtsv --rep ./mnt/data/MSConvert/ --out ./mnt/data/MSConvert/FlashLFQVignette_Docker_Output
