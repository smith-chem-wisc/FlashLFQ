namespace FlashLFQ
{
    class IsotopeCluster
    {
        public readonly MzBinElement peakWithScan;
        public readonly int chargeState;

        public IsotopeCluster(MzBinElement peakWithScan, int chargeState)
        {
            this.peakWithScan = peakWithScan;
            this.chargeState = chargeState;
        }
    }
}
