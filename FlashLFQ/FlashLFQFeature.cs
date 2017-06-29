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

        public static string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("File Name" + "\t");
                sb.Append("Base Sequence" + "\t");
                sb.Append("Full Sequence" + "\t");
                sb.Append("Peptide Monoisotopic Mass" + "\t");
                sb.Append("MS2 Retention Time" + "\t");
                sb.Append("Precursor Charge" + "\t");
                sb.Append("Theoretical MZ" + "\t");
                sb.Append("Peak intensity" + "\t");
                sb.Append("Peak RT Start" + "\t");
                sb.Append("Peak RT Apex" + "\t");
                sb.Append("Peak RT End" + "\t");
                sb.Append("Peak MZ" + "\t");
                sb.Append("Peak Charge" + "\t");
                sb.Append("Apex Signal-To-Background" + "\t");
                sb.Append("Peak Detection Type" + "\t");
                sb.Append("PSMs Mapped" + "\t");
                sb.Append("Base Sequences Mapped" + "\t");
                sb.Append("Full Sequences Mapped");
                return sb.ToString();
            }
        }

        public void CalculateIntensityForThisFeature(bool integrate)
        {
            if (isotopeClusters.Any())
            {
                //apexPeak = isotopeClusters.Where(p => p.isotopeClusterIntensity == isotopeClusters.Max(v => v.isotopeClusterIntensity)).FirstOrDefault();

                double monoisotopicPeakMax = isotopeClusters.Select(p => p.peakWithScan.backgroundSubtractedIntensity).Max();
                apexPeak = isotopeClusters.Where(p => p.peakWithScan.backgroundSubtractedIntensity == monoisotopicPeakMax).FirstOrDefault();

                if (integrate)
                    intensity = isotopeClusters.Select(p => p.isotopeClusterIntensity).Sum();
                else
                    intensity = apexPeak.isotopeClusterIntensity;
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
