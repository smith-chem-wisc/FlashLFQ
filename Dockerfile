## Base image is the Alpine Linux distro with .NET Core runtime
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine AS build

## updates Linux package manager
RUN apk update

## This downloads the latest FlashLFQ release from the web
## Uncomment lines 10-11 if you want to download+build the latest release version
## "wget" is a package for downloading files
##RUN apk add wget \
##	&& wget https://github.com/smith-chem-wisc/FlashLFQ/releases/latest/download/FlashLFQ_CommandLine.zip .

## This copies FlashLFQ_CommandLine.zip from the current directory into the Docker image
## This is used for debugging and AppVeyor
COPY FlashLFQ_CommandLine.zip /

## Install dependencies and unzip FlashLFQ
## "unzip" is an de-archiving package
RUN apk add unzip \
	&& unzip FlashLFQ_CommandLine.zip

## Set the entrypoint of the Docker image to CMD.dll
ENTRYPOINT ["dotnet", "CMD.dll"]

## Build example:
## docker build -t flashlfq .

## Run example:
## docker run --rm -v C:/Data/Vignette_FlashLFQ:/mnt/data flashlfq --idt ./mnt/data/MSConvert/AllPSMs.psmtsv --rep ./mnt/data/MSConvert/ --out ./mnt/data/MSConvert/FlashLFQVignette_Docker_Output
