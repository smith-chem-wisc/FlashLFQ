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
            List<ScanHeaderInfo> shi = new List<ScanHeaderInfo>();
            switch (GetDataFileType(fullFilePathWithExtension))
            {
                case DataFileType.Thermo:
                    ThermoRawFileReader staticRaw = ThermoRawFileReader.LoadAllStaticData(fullFilePathWithExtension);
                    foreach (MsDataScan item in staticRaw)
                    {
                        shi.Add(new ScanHeaderInfo(fullFilePathWithExtension, filename, item.OneBasedScanNumber, item.RetentionTime));
                    }
                    break;
                case DataFileType.mzML:
                    List<MsDataScan> mzmlDataScans = Mzml.LoadAllStaticData(fullFilePathWithExtension).GetAllScansList();
                    foreach (MsDataScan item in mzmlDataScans)
                    {
                        shi.Add(new ScanHeaderInfo(fullFilePathWithExtension, filename, item.OneBasedScanNumber, item.RetentionTime));
                    }
                    break;
                case DataFileType.mgf:
                    List<MsDataScan> mgfDataScans = Mgf.LoadAllStaticData(fullFilePathWithExtension).GetAllScansList();
                    foreach (MsDataScan item in mgfDataScans)
                    {
                        shi.Add(new ScanHeaderInfo(fullFilePathWithExtension, filename, item.OneBasedScanNumber, item.RetentionTime));
                    }
                    break;
                case DataFileType.unknown:
                default:
                    break;
            }
            return shi;
        }

        //public static double RetentionTimeFromScanNumber(string fullFilePathWithExtension, int oneBasedScanNumber)
        //{
        //    switch (GetDataFileType(fullFilePathWithExtension))
        //    {
        //        case DataFileType.Thermo:
        //            ThermoDynamicData thermoDynamicConnection = null;
        //            try
        //            {
        //                thermoDynamicConnection = new ThermoDynamicData(fullFilePathWithExtension);
        //                MsDataScan scan = thermoDynamicConnection.GetOneBasedScanFromDynamicConnection(oneBasedScanNumber, null);
        //                thermoDynamicConnection.CloseDynamicConnection();
        //                return scan.RetentionTime;
        //            }
        //            catch (FileNotFoundException)
        //            {
        //                if (thermoDynamicConnection != null)
        //                {
        //                    thermoDynamicConnection.CloseDynamicConnection();
        //                    Console.WriteLine("FlashLFQ Error: Can't find data file" + fullFilePathWithExtension + "\n");
        //                }

        //                return Double.NaN;
        //            }
        //            catch (Exception e)
        //            {
        //                if (thermoDynamicConnection != null)
        //                {
        //                    thermoDynamicConnection.CloseDynamicConnection();
        //                    throw new MzLibException("FlashLFQ Error: Problem opening data file " + fullFilePathWithExtension + "; " + e.Message);
        //                }
        //                return Double.NaN;
        //            }
        //        case DataFileType.mzML:
        //            MzmlDynamicData mzmlDynamicConnection = null;
        //            try
        //            {
        //                mzmlDynamicConnection = new MzmlDynamicData(fullFilePathWithExtension);
        //                MsDataScan scan = mzmlDynamicConnection.GetOneBasedScanFromDynamicConnection(oneBasedScanNumber, null);
        //                mzmlDynamicConnection.CloseDynamicConnection();
        //                return scan.RetentionTime;
        //            }
        //            catch (FileNotFoundException)
        //            {
        //                if (mzmlDynamicConnection != null)
        //                {
        //                    mzmlDynamicConnection.CloseDynamicConnection();
        //                    Console.WriteLine("FlashLFQ Error: Can't find data file" + fullFilePathWithExtension + "\n");
        //                }

        //                return Double.NaN;
        //            }
        //            catch (Exception e)
        //            {
        //                if (mzmlDynamicConnection != null)
        //                {
        //                    mzmlDynamicConnection.CloseDynamicConnection();
        //                    throw new MzLibException("FlashLFQ Error: Problem opening data file " + fullFilePathWithExtension + "; " + e.Message);
        //                }
        //                return Double.NaN;
        //            }
        //        case DataFileType.mgf:
        //            MgfDynamicData mgfDynamicConnection = null;
        //            try
        //            {
        //                mgfDynamicConnection = new MgfDynamicData(fullFilePathWithExtension);
        //                MsDataScan scan = mgfDynamicConnection.GetOneBasedScanFromDynamicConnection(oneBasedScanNumber, null);
        //                mgfDynamicConnection.CloseDynamicConnection();
        //                return scan.RetentionTime;
        //            }
        //            catch (FileNotFoundException)
        //            {
        //                if (mgfDynamicConnection != null)
        //                {
        //                    mgfDynamicConnection.CloseDynamicConnection();
        //                    Console.WriteLine("FlashLFQ Error: Can't find data file" + fullFilePathWithExtension + "\n");
        //                }

        //                return Double.NaN;
        //            }
        //            catch (Exception e)
        //            {
        //                if (mgfDynamicConnection != null)
        //                {
        //                    mgfDynamicConnection.CloseDynamicConnection();
        //                    throw new MzLibException("FlashLFQ Error: Problem opening data file " + fullFilePathWithExtension + "; " + e.Message);
        //                }
        //                return Double.NaN;
        //            }
        //        case DataFileType.unknown:
        //        default:
        //            throw new MzLibException("FlashLFQ Error: Unknown data file extension (.raw, .mzML, .mgf expected): " + fullFilePathWithExtension);
        //    }
        //}

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