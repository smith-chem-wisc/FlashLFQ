namespace FlashLFQ
{
    public class FlashLFQIsotopeCluster
    {
        public readonly FlashLFQMzBinElement peakWithScan;
        public readonly int chargeState;

        public FlashLFQIsotopeCluster(FlashLFQMzBinElement peakWithScan, int chargeState)
        {
            this.peakWithScan = peakWithScan;
            this.chargeState = chargeState;
        }
    }
}
