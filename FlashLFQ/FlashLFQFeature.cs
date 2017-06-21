using Chemistry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlashLFQ
{
    public class FlashLFQFeature
    {
        public double intensity;
        public FlashLFQIsotopeCluster apexPeak;
        public bool isMbrFeature;
        public string fileName = "";
        public List<FlashLFQIdentification> identifyingScans;
        public List<FlashLFQIsotopeCluster> isotopeClusters;
        public int numIdentificationsByBaseSeq { get; private set; }
        public int numIdentificationsByFullSeq { get; private set; }

        public FlashLFQFeature()
        {
            numIdentificationsByBaseSeq = 1;
            numIdentificationsByFullSeq = 1;
            identifyingScans = new List<FlashLFQIdentification>();
            isotopeClusters = new List<FlashLFQIsotopeCluster>();
        }

        public void CalculateIntensityForThisFeature(string fileName, bool integrate)
        {
            this.fileName = fileName;
            if (isotopeClusters.Any())
            {
                double featureApexIntensity = isotopeClusters.Select(p => p.isotopeClusterIntensity).Max();
                apexPeak = isotopeClusters.Where(p => p.isotopeClusterIntensity == featureApexIntensity).FirstOrDefault();

                // apex intensity
                if (!integrate)
                {
                    /*
                    var peaksGroupedByChargeState = isotopeClusters.GroupBy(p => p.chargeState);

                    foreach (var chargeState in peaksGroupedByChargeState)
                        intensity += chargeState.Select(p => p.isotopeClusterIntensity).Max();
                    */

                    intensity = featureApexIntensity;
                }
                
                // integrate, calculate half max
                if (integrate)
                {
                    intensity = isotopeClusters.Select(p => p.isotopeClusterIntensity).Sum();

                    /*
                    var peaksGroupedByChargeState = isotopeClusters.GroupBy(p => p.chargeState);

                    foreach (var chargeState in peaksGroupedByChargeState)
                    {
                        var peaksList = chargeState.OrderBy(p => p.peakWithScan.retentionTime).ToList();
                        for(int i = 1; i < peaksList.Count; i++)
                        {
                            intensity += ((peaksList[i].peakWithScan.backgroundSubtractedIntensity + peaksList[i - 1].peakWithScan.backgroundSubtractedIntensity) / 2.0) * (peaksList[i].peakWithScan.retentionTime - peaksList[i - 1].peakWithScan.retentionTime);
                        }
                    }
                    */
                }
            }
        }

        public void MergeFeatureWith(IEnumerable<FlashLFQFeature> otherFeatures)
        {
            foreach (var feature in otherFeatures)
            {
                if (feature != this)
                {
                    this.identifyingScans = this.identifyingScans.Union(feature.identifyingScans).Distinct().ToList();
                    this.numIdentificationsByBaseSeq = identifyingScans.Select(v => v.BaseSequence).Distinct().Count();
                    this.numIdentificationsByFullSeq = identifyingScans.Select(v => v.FullSequence).Distinct().Count();
                    feature.intensity = -1;
                }
            }
        }
        
        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(fileName + "\t");
            sb.Append(string.Join("|", identifyingScans.Select(p => p.BaseSequence).Distinct()) + '\t');
            sb.Append(string.Join("|", identifyingScans.Select(p => p.FullSequence).Distinct()) + '\t');
            sb.Append("" + identifyingScans.First().monoisotopicMass + '\t');
            sb.Append("" + identifyingScans.First().ms2RetentionTime + '\t');
            sb.Append("" + identifyingScans.First().chargeState + '\t');
            sb.Append("" + ClassExtensions.ToMz(identifyingScans.First().monoisotopicMass, identifyingScans.First().chargeState) + '\t');
            sb.Append("" + intensity + "\t");

            if (apexPeak != null)
            {
                sb.Append("" + isotopeClusters.Select(p => p.peakWithScan.retentionTime).Min() + "\t");
                sb.Append("" + apexPeak.peakWithScan.retentionTime + "\t");
                sb.Append("" + isotopeClusters.Select(p => p.peakWithScan.retentionTime).Max() + "\t");

                sb.Append("" + apexPeak.peakWithScan.mainPeak.Mz + "\t");
                sb.Append("" + apexPeak.chargeState + "\t");
                sb.Append("" + (apexPeak.peakWithScan.signalToBackgroundRatio) + "\t");
            }
            else
            {
                sb.Append("" + "-" + "\t");
                sb.Append("" + "-" + "\t");
                sb.Append("" + "-" + "\t");

                sb.Append("" + "-" + "\t");
                sb.Append("" + "-" + "\t");
                sb.Append("" + 0 + "\t");
            }
            
            if (isMbrFeature)
                sb.Append("" + "MBR" + "\t");
            else
                sb.Append("" + "MSMS" + "\t");

            sb.Append("" + identifyingScans.Count + "\t");
            sb.Append("" + numIdentificationsByBaseSeq + "\t");
            sb.Append("" + numIdentificationsByFullSeq);

            return sb.ToString();
        }
    }
}
