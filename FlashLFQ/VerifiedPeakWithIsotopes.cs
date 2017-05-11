using MassSpectrometry;
using System.Linq;

namespace FlashLFQ
{
    class VerifiedPeakWithIsotopes
    {
        public readonly double peakRT;
        public readonly double peakMZ;
        public readonly double apexIntensity;
        public readonly double summedIntensity;
        public readonly double[] isotopeMZs;
        public readonly double[] isotopeIntensities;

        public VerifiedPeakWithIsotopes(IMzPeak peak, IMsDataScan<IMzSpectrum<IMzPeak>> scan, IMzPeak[] isotopePeaks)
        {
            peakRT = scan.RetentionTime;
            peakMZ = peak.Mz;
            apexIntensity = peak.Intensity;
            //apexIntensity = isotopePeaks.Select(p => p.Intensity).Max();

            isotopeMZs = new double[isotopePeaks.Length];
            isotopeIntensities = new double[isotopePeaks.Length];

            for (int i = 0; i < isotopePeaks.Length; i++)
            {
                isotopeMZs[i] = isotopePeaks[i].Mz;
                isotopeIntensities[i] = isotopePeaks[i].Intensity;
            }

            summedIntensity = isotopeIntensities.Sum();
        }
    }
}
