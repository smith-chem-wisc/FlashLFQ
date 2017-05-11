using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlashLFQ
{
    class Feature
    {
        public double intensity;
        public IsotopeCluster apexPeak;
        public string featureType;
        public string fileName = "";
        public List<Identification> identifyingScans;
        public List<IsotopeCluster> isotopeClusters;

        public Feature()
        {
            identifyingScans = new List<Identification>();
            isotopeClusters = new List<IsotopeCluster>();
        }

        public void CalculateIntensityForThisFeature(int fileIndex, bool integrate)
        {
            if (isotopeClusters.Any())
            {
                double featureApexIntensity = isotopeClusters.Select(p => p.peakWithScan.backgroundSubtractedIntensity).Max();
                apexPeak = isotopeClusters.Where(p => p.peakWithScan.backgroundSubtractedIntensity == featureApexIntensity).FirstOrDefault();

                // apex intensity
                if (!integrate)
                    intensity = featureApexIntensity;

                
                // integrate, calculate half max
                if (integrate)
                {
                    //intensity = isotopeClusters.Select(p => p.peakWithScan.backgroundSubtractedIntensity).Sum();

                    var peaksGroupedByChargeState = isotopeClusters.GroupBy(p => p.chargeState);

                    foreach (var chargeState in peaksGroupedByChargeState)
                    {
                        var peaksList = chargeState.OrderBy(p => p.peakWithScan.scan.RetentionTime).ToList();
                        for(int i = 1; i < peaksList.Count; i++)
                        {
                            intensity += ((peaksList[i].peakWithScan.backgroundSubtractedIntensity + peaksList[i - 1].peakWithScan.backgroundSubtractedIntensity) / 2.0) * (peaksList[i].peakWithScan.scan.RetentionTime - peaksList[i - 1].peakWithScan.scan.RetentionTime);
                        }
                    }


                    /*
                    // calculate area of peak with triangle approximation and LHM/RHM
                    var peaksGroupedByChargeState = isotopeClusters.GroupBy(p => p.chargeState);

                    foreach (var chargeState in peaksGroupedByChargeState)
                    {
                        var peaksList = chargeState.OrderBy(p => p.peakWithScan.scan.RetentionTime).ToList();
                        double apexIntensityForThisChargeState = peaksList.Max(p => p.peakWithScan.backgroundSubtractedIntensity);
                        var apexPeakForThisChargeState = peaksList.Where(p => p.peakWithScan.backgroundSubtractedIntensity == apexIntensityForThisChargeState).First();

                        IsotopeCluster leftHalfMaxPeak = null;
                        IsotopeCluster rightHalfMaxPeak = null;

                        int indexOfApexPeak = peaksList.IndexOf(apexPeakForThisChargeState);

                        for (int i = indexOfApexPeak; i >= 0; i--)
                            if ((peaksList[i].peakWithScan.backgroundSubtractedIntensity) <= 0.5 * apexIntensityForThisChargeState)
                            {
                                leftHalfMaxPeak = peaksList[i];
                                break;
                            }

                        for (int i = indexOfApexPeak; i < peaksList.Count; i++)
                            if ((peaksList[i].peakWithScan.backgroundSubtractedIntensity) <= 0.5 * apexIntensityForThisChargeState)
                            {
                                rightHalfMaxPeak = peaksList[i];
                                break;
                            }

                        // if half max's are not findable, use the peak at the end
                        if (leftHalfMaxPeak == null)
                            leftHalfMaxPeak = peaksList.First();
                        if (rightHalfMaxPeak == null)
                            rightHalfMaxPeak = peaksList.Last();

                        double leftSlope = (apexIntensityForThisChargeState - leftHalfMaxPeak.peakWithScan.backgroundSubtractedIntensity) / (apexPeakForThisChargeState.peakWithScan.scan.RetentionTime - leftHalfMaxPeak.peakWithScan.scan.RetentionTime);
                        double rightSlope = (apexIntensityForThisChargeState - rightHalfMaxPeak.peakWithScan.backgroundSubtractedIntensity) / (apexPeakForThisChargeState.peakWithScan.scan.RetentionTime - rightHalfMaxPeak.peakWithScan.scan.RetentionTime);

                        double leftApproxYIntercept = ((0 - leftHalfMaxPeak.peakWithScan.backgroundSubtractedIntensity) / leftSlope) + leftHalfMaxPeak.peakWithScan.scan.RetentionTime;
                        double rightApproxYIntercept = ((0 - rightHalfMaxPeak.peakWithScan.backgroundSubtractedIntensity) / rightSlope) + rightHalfMaxPeak.peakWithScan.scan.RetentionTime;

                        if (double.IsNaN(rightApproxYIntercept))
                            rightApproxYIntercept = rightHalfMaxPeak.peakWithScan.scan.RetentionTime;
                        if (double.IsNaN(leftApproxYIntercept))
                            leftApproxYIntercept = leftHalfMaxPeak.peakWithScan.scan.RetentionTime;

                        double baseOfTriangle = rightApproxYIntercept - leftApproxYIntercept;

                        if (baseOfTriangle != 0)
                            intensity += 0.5 * baseOfTriangle * apexIntensityForThisChargeState;
                        else
                            intensity += apexIntensityForThisChargeState;
                    }
                    */
                }
            }
        }

        public void MergeFeatureWith(IEnumerable<Feature> otherFeatures)
        {
            foreach (var feature in otherFeatures)
            {
                if (feature != this)
                {
                    this.identifyingScans = this.identifyingScans.Union(feature.identifyingScans).ToList();
                    feature.intensity = -1;
                }
            }
        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(fileName + "\t");
            sb.Append("" + identifyingScans.First().BaseSequence + '\t');
            sb.Append("" + identifyingScans.First().FullSequence + '\t');
            sb.Append("" + identifyingScans.First().monoisotopicMass + '\t');
            sb.Append("" + identifyingScans.First().ms2RetentionTime + '\t');
            sb.Append("" + identifyingScans.First().initialChargeState + '\t');
            sb.Append("" + intensity + "\t");

            if (apexPeak != null)
            {
                sb.Append("" + apexPeak.peakWithScan.scan.RetentionTime + "\t");
                sb.Append("" + apexPeak.peakWithScan.mainPeak.Mz + "\t");
                sb.Append("" + apexPeak.chargeState + "\t");
                sb.Append("" + (apexPeak.peakWithScan.backgroundSubtractedIntensity) + "\t");
            }
            else
            {
                sb.Append("" + 0 + "\t");
                sb.Append("" + 0 + "\t");
                sb.Append("" + 0 + "\t");
                sb.Append("" + 0 + "\t");
            }

            sb.Append("" + featureType + "\t");
            sb.Append("" + identifyingScans.Count + "\t");
            sb.Append("" + identifyingScans.Select(p => p.FullSequence).Distinct().Count() + "\t");
            

            return sb.ToString();
        }
    }
}
