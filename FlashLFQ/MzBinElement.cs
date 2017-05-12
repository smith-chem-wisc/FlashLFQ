using MassSpectrometry;

namespace FlashLFQ
{
    class MzBinElement
    {
        public readonly IMzPeak mainPeak;
        public IMsDataScan<IMzSpectrum<IMzPeak>> scan { get; private set; }
        public readonly double backgroundIntensity;
        public readonly int zeroBasedIndexOfPeakInScan;
        public readonly double backgroundSubtractedIntensity;
        public readonly double signalToBackgroundRatio;
        public readonly double retentionTime;
        public readonly int oneBasedScanNumber;

        public MzBinElement(IMzPeak peak, IMsDataScan<IMzSpectrum<IMzPeak>> scan, double backgroundIntensity, int index)
        {
            mainPeak = peak;
            this.scan = scan;
            this.backgroundIntensity = backgroundIntensity;
            zeroBasedIndexOfPeakInScan = index;
            backgroundSubtractedIntensity = peak.Intensity - backgroundIntensity;
            signalToBackgroundRatio = peak.Intensity / backgroundIntensity;
            oneBasedScanNumber = scan.OneBasedScanNumber;
            retentionTime = scan.RetentionTime;
        }

        public void Compress()
        {
            scan = null;
        }
    }
}
