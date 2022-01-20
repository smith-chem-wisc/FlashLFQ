using IO.Mgf;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.IO;

namespace Util
{
    public class ScanInfoRecovery
    {
        private enum DataFileType
        { Thermo, mzML, mgf, unknown }

        public static List<ScanHeaderInfo> FileScanHeaderInfo(string fullFilePathWithExtension)
        {
            string filename = Path.GetFileNameWithoutExtension(fullFilePathWithExtension);
            List<ScanHeaderInfo> scanHeaderInfoList = new();
            switch (GetDataFileType(fullFilePathWithExtension))
            {
                case DataFileType.Thermo:
                    ThermoRawFileReader staticRaw = ThermoRawFileReader.LoadAllStaticData(fullFilePathWithExtension);
                    foreach (MsDataScan item in staticRaw)
                    {
                        scanHeaderInfoList.Add(new ScanHeaderInfo(fullFilePathWithExtension, filename, item.OneBasedScanNumber, item.RetentionTime));
                    }
                    break;
                case DataFileType.mzML:
                    List<MsDataScan> mzmlDataScans = Mzml.LoadAllStaticData(fullFilePathWithExtension).GetAllScansList();
                    foreach (MsDataScan item in mzmlDataScans)
                    {
                        scanHeaderInfoList.Add(new ScanHeaderInfo(fullFilePathWithExtension, filename, item.OneBasedScanNumber, item.RetentionTime));
                    }
                    break;
                case DataFileType.mgf:
                    List<MsDataScan> mgfDataScans = Mgf.LoadAllStaticData(fullFilePathWithExtension).GetAllScansList();
                    foreach (MsDataScan item in mgfDataScans)
                    {
                        scanHeaderInfoList.Add(new ScanHeaderInfo(fullFilePathWithExtension, filename, item.OneBasedScanNumber, item.RetentionTime));
                    }
                    break;
                case DataFileType.unknown:
                default:
                    break;
            }
            return scanHeaderInfoList;
        }
        private static DataFileType GetDataFileType(string fullFilePathWithExtension)
        {
            string g = Path.GetExtension(fullFilePathWithExtension).ToLowerInvariant();
            return Path.GetExtension(fullFilePathWithExtension).ToLowerInvariant() switch
            {
                ".raw" => DataFileType.Thermo,
                ".mzml" => DataFileType.mzML,
                ".mgf" => DataFileType.mgf,
                _ => DataFileType.unknown,
            };
        }
    }
}