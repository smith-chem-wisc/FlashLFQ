## Base image is the Alpine Linux distro with .NET Core runtime
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine AS build

## Copies FlashLFQ_CommandLine.zip from the current directory into the Docker image
COPY FlashLFQ_CommandLine.zip /

## Install dependencies and unzip FlashLFQ
## "unzip" is an de-archiving package
RUN apk update \
	&& apk add unzip \
	&& unzip /FlashLFQ_CommandLine.zip

## Set the entrypoint of the Docker image to CMD.dll
ENTRYPOINT ["dotnet", "CMD.dll"]

## Build example:
## docker build -t flashlfq .

## Run example:
## docker run --rm -v C:/Data/Vignette_FlashLFQ:/mnt/data flashlfq --idt ./mnt/data/MSConvert/AllPSMs.psmtsv --rep ./mnt/data/MSConvert/ --out ./mnt/data/MSConvert/FlashLFQVignette_Docker_Output
