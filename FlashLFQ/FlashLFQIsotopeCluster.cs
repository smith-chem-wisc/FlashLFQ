namespace Engine
{
    public class FlashLFQIsotopeCluster
    {
        public readonly FlashLFQMzBinElement peakWithScan;
        public readonly int chargeState;
        public double isotopeClusterIntensity;

        public FlashLFQIsotopeCluster(FlashLFQMzBinElement monoisotopicPeak, int chargeState, double intensity)
        {
            this.peakWithScan = monoisotopicPeak;
            this.chargeState = chargeState;
            this.isotopeClusterIntensity = intensity;
        }

        public override string ToString()
        {
            return isotopeClusterIntensity + "; " + peakWithScan.retentionTime;
        }
    }
}
