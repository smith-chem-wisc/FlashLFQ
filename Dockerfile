## Base image is the Alpine Linux distro with .NET Core runtime
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS build

## Copies contents of the "publish" folder into the Docker image
ADD CMD/bin/Release/net6.0/publish/ /flashlfq/

## Set the entrypoint of the Docker image to CMD.dll
ENTRYPOINT ["dotnet", "/flashlfq/CMD.dll"]

## Build example:
## docker build -t flashlfq .

## Run example:
## docker run --rm -v C:/Data/Vignette_FlashLFQ:/mnt/data flashlfq --idt ./mnt/data/MSConvert/AllPSMs.psmtsv --rep ./mnt/data/MSConvert/ --out ./mnt/data/MSConvert/FlashLFQVignette_Docker_Output
