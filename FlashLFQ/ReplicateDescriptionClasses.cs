using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashLFQ
{
    public class PeptideQuantities
    {
        public List<Condition> conditions;

        public PeptideQuantities(List<RawFileInfo> rawFileInfos)
        {
            conditions = new List<Condition>();
            var conds = rawFileInfos.GroupBy(p => p.condition).ToList();

            foreach(var condition in conds)
            {
                var bioreps = condition.ToList().GroupBy(p => p.biorep);
                List<BiologicalReplicate> listOfBioreps = new List<BiologicalReplicate>();

                foreach (var biorep in bioreps)
                {
                    var fractions = biorep.ToList().GroupBy(p => p.fraction);
                    List<Fraction> listOfFractions = new List<Fraction>();

                    foreach (var fraction in fractions)
                    {
                        var techreps = fraction.ToList().GroupBy(p => p.techrep);
                        List<TechnicalReplicate> listOfTechreps = new List<TechnicalReplicate>();

                        foreach (var techrep in techreps)
                        {
                            if (techrep.Count() > 1)
                            {
                                // throw exception
                            }

                            listOfTechreps.Add(new TechnicalReplicate(techrep.First(), 0, techrep.Key));
                        }

                        listOfFractions.Add(new Fraction(fraction.Key, listOfTechreps));
                    }

                    listOfBioreps.Add(new BiologicalReplicate(biorep.Key, listOfFractions));
                }

                conditions.Add(new Condition(condition.Key, listOfBioreps));
            }
        }

        public double GetIntensity()
        {
            return conditions.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string condition)
        {
            var cond = conditions.Where(p => p.conditionName.Equals(condition));
            return cond.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string condition, string biorep)
        {
            var cond = conditions.Where(p => p.conditionName.Equals(condition));
            // this is a better way - change to this later
            // return cond.Sum(p => p.GetIntensity(biorep));
            
            var bio = cond.SelectMany(p => p.bioreps).Where(p => p.biorepName.Equals(biorep));
            return bio.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string condition, string biorep, string fraction)
        {
            var cond = conditions.Where(p => p.conditionName.Equals(condition));
            var bio = cond.SelectMany(p => p.bioreps).Where(p => p.biorepName.Equals(biorep));
            var frac = bio.SelectMany(p => p.fractions).Where(p => p.fractionName.Equals(fraction));
            return frac.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string condition, string biorep, string fraction, string techrep)
        {
            var cond = conditions.Where(p => p.conditionName.Equals(condition));
            var bio = cond.SelectMany(p => p.bioreps).Where(p => p.biorepName.Equals(biorep));
            var frac = bio.SelectMany(p => p.fractions).Where(p => p.fractionName.Equals(fraction));
            return frac.SelectMany(p => p.techreps).Where(p => p.techrepName.Equals(techrep)).FirstOrDefault().intensity;
        }

        public bool IsNormalizable(List<string> combos) // must be in every biorep. could be changed to every condition. 
        {
            int comboIntensityCounts = 0;

            foreach (string combo in combos)
            {
                string[] bc = combo.ToString().Split('-');
                string cond = bc[0];
                string br = bc[1];

                List<Condition> ccc = this.conditions.Where(ppp => ppp.conditionName.Equals(cond)).ToList(); //here we are using the original list and applying all the normalization factors (not using the reduced list)
                List<BiologicalReplicate> bbb = ccc.SelectMany(ppp => ppp.bioreps).Where(ppp => ppp.biorepName.Equals(br)).ToList();
                foreach (BiologicalReplicate b in bbb)           
                    if (b.GetIntensity() > 0)
                        comboIntensityCounts++;                           
            }

            if (comboIntensityCounts == combos.Count)
                return true;
            else
                return false;
        }
    }

    public class Condition
    {
        public readonly string conditionName;
        public readonly List<BiologicalReplicate> bioreps;

        public Condition(string conditionName, List<BiologicalReplicate> bioreps)
        {
            this.conditionName = conditionName;
            this.bioreps = bioreps;
        }

        public double GetIntensity()
        {
            return bioreps.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string biorep)
        {
            return bioreps.Where(p => p.biorepName.Equals(biorep)).Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string biorep, string fraction)
        {
            var bio = bioreps.Where(p => p.biorepName.Equals(biorep));
            var frac = bio.SelectMany(p => p.fractions).Where(p => p.fractionName.Equals(fraction));
            return frac.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string biorep, string fraction, string techrep)
        {
            var bio = bioreps.Where(p => p.biorepName.Equals(biorep));
            var frac = bio.SelectMany(p => p.fractions).Where(p => p.fractionName.Equals(fraction));
            return frac.SelectMany(p => p.techreps).Where(p => p.techrepName.Equals(techrep)).FirstOrDefault().intensity;
        }
    }

    public class BiologicalReplicate
    {
        public readonly string biorepName;
        public readonly List<Fraction> fractions;

        public BiologicalReplicate(string biorepName, List<Fraction> fractions)
        {
            this.biorepName = biorepName;
            this.fractions = fractions;
        }

        public double GetIntensity()
        {
            return fractions.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string fraction)
        {
            var frac = fractions.Where(p => p.fractionName.Equals(fraction));
            return frac.Sum(p => p.GetIntensity());
        }

        public double GetIntensity(string fraction, string techrep)
        {
            var frac = fractions.Where(p => p.fractionName.Equals(fraction));
            return frac.SelectMany(p => p.techreps).Where(p => p.techrepName.Equals(techrep)).FirstOrDefault().intensity;
        }
    }

    public class Fraction
    {
        public readonly string fractionName;
        public readonly List<TechnicalReplicate> techreps;
        public double normalizationFactor { get; set; }

        public Fraction(string fractionName, List<TechnicalReplicate> techreps)
        {
            this.fractionName = fractionName;
            this.techreps = techreps;
            normalizationFactor = 1.0;
        }

        public void SetNormalizationFactor(double normalizationFactor)
        {
            this.normalizationFactor = normalizationFactor;
        }

        public double GetIntensity()
        {
            return techreps.Sum(p => p.intensity) * normalizationFactor;
        }
    }
    
    public class TechnicalReplicate
    {
        public readonly RawFileInfo rawFile;
        public readonly string techrepName;
        public double intensity { get; private set; }
        public string detectionType;

        public TechnicalReplicate(RawFileInfo rawFile, double intensity, string techrepName)
        {
            this.rawFile = rawFile;
            this.intensity = intensity;
            this.techrepName = techrepName;
        }

        public void SetPeptideIntensity(double intensity)
        {
            this.intensity = intensity;
        }

        public void SetPeptideIntensity(double intensity, string dt)
        {
            this.intensity = intensity;
            this.detectionType = dt;
        }
    }
}
