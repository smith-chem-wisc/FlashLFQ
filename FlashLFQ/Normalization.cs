using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using Accord.Math;
using LpSolveDotNet;


namespace FlashLFQ
{
    public class Normalization
    {

        public List<Peptide> Normalize(List<Peptide> peptideFeatures, List<RawFileInfo> fpCBFT)
        {
            List<string> cdtns = new List<string>();
            List<string> bioreps = new List<string>();
            List<string> fractions = new List<string>();
            foreach (RawFileInfo rfi in fpCBFT)
            {
                if (!cdtns.Contains(rfi.condition))
                    cdtns.Add(rfi.condition);
                if (!bioreps.Contains(rfi.biorep))
                    bioreps.Add(rfi.biorep);
                if (!fractions.Contains(rfi.fraction))
                    fractions.Add(rfi.fraction);
            }

            cdtns.GroupBy(i => i).Select(i => i.First()).ToList();
            bioreps.GroupBy(i => i).Select(i => i.First()).ToList();
            fractions.GroupBy(i => i).Select(i => i.First()).ToList();

            List<string> combo = new List<string>(); //we're treating conditions as bioreps for the purpose of normalization. Assumption being that the majoring of proteins accross bioreps and conditions is unchanging.
            foreach (string cc in cdtns)
            {
                foreach (string bb in bioreps)
                {
                    combo.Add(cc + "-" + bb);
                }
            }

            if (combo.Count > 0) // we only normalize if there is more than one condiiton or biorep
            {
                List<Peptide> featuresForNormalization = GetNormalizableFeatures(peptideFeatures, combo); //reduce the list to only those features present in every condition or every biorep

                if (featuresForNormalization.Count > 0) // that can be changed to a percentage of the total if that makes more sense. 75%???
                {
                    int featureCount = featuresForNormalization.Count;

                    var a = new double[featureCount, combo.Count, fractions.Count];

                    for (int b = 0; b < combo.Count; b++)
                    {
                        for (int f = 0; f < fractions.Count; f++)
                            for (int p = 0; p < featureCount; p++)
                            {
                                string[] bc = combo[b].ToString().Split('-');
                                string cond = bc[0];
                                string br = bc[1];
                                string fr = fractions[f];

                                double intensity = 0;
                                try
                                {
                                    List<Condition> ccc = featuresForNormalization[p].quantities.conditions.Where(ppp => ppp.conditionName.Equals(cond)).ToList();
                                    List<BiologicalReplicate> bbb = ccc.SelectMany(ppp => ppp.bioreps).Where(ppp => ppp.biorepName.Equals(br)).ToList();
                                    List<Fraction> fff = bbb.SelectMany(ppp => ppp.fractions).Where(ppp => ppp.fractionName.Equals(fr)).ToList();
                                    Fraction thisF = fff.Where(ppp => ppp.fractionName == fr).First();
                                    intensity = thisF.GetIntensity();
                                }
                                catch
                                {
                                    intensity = 0;
                                }

                                a[p, b, f] = intensity;
                            }
                    }

                    double[] myCoefficients = new double[combo.Count * fractions.Count + featureCount * combo.Count];

                    myCoefficients = SolveWithLpSolve(a, featureCount, combo.Count, fractions.Count);

                    int counter = 0;
                    for (int b = 0; b < combo.Count; b++)
                    {
                        for (int f = 0; f < fractions.Count; f++)
                        {
                            for (int p = 0; p < peptideFeatures.Count; p++)//using a different counter here because we have to put normalization factors into every peptide feature
                            {
                                string[] bc = combo[b].ToString().Split('-');
                                string cond = bc[0];
                                string br = bc[1];
                                string fr = fractions[f];

                                try
                                {
                                    List<Condition> ccc = peptideFeatures[p].quantities.conditions.Where(ppp => ppp.conditionName.Equals(cond)).ToList(); //here we are using the original list and applying all the normalization factors (not using the reduced list)
                                    List<BiologicalReplicate> bbb = ccc.SelectMany(ppp => ppp.bioreps).Where(ppp => ppp.biorepName.Equals(br)).ToList();
                                    List<Fraction> fff = bbb.SelectMany(ppp => ppp.fractions).Where(ppp => ppp.fractionName.Equals(fr)).ToList();
                                    Fraction thisF = fff.Where(ppp => ppp.fractionName == fr).First();
                                    thisF.normalizationFactor = myCoefficients[counter];
                                }
                                catch
                                {

                                }
                            }
                            counter++;
                        }
                    }
                }
            }         

            return peptideFeatures;

        }

        private static List<Peptide> GetNormalizableFeatures(List<Peptide> features, List<string> combos)
        {
            List<Peptide> nF = new List<Peptide>();           

            foreach (Peptide p in features)
            {
                if (p.quantities.IsNormalizable(combos))
                    nF.Add(p);
            }

            return nF;
        }

        private static double[] SolveWithLpSolve(double[,,] a, int numP, int numB, int numF)
        {
            LpSolve.Init();
            LpSolve lpSolve = LpSolve.make_lp(0, numB * numF + numP * numB);


            // Add constraints
            for (int b = 0; b < numB; b++)
            {
                for (int p = 0; p < numP; p++)
                {
                    double[] coefs = new double[1 + numB * numF + numP * numB];
                    for (int b_ = 0; b_ < numB; b_++)
                    {
                        if (b_ == b)
                            for (int f = 0; f < numF; f++)
                                coefs[1 + b_ * numF + f] = a[p, b_, f] * (numB - 1);
                        else
                            for (int f = 0; f < numF; f++)
                                coefs[1 + b_ * numF + f] = -a[p, b_, f];
                    }
                    coefs[1 + numB * numF + b * numP + p] = -1;
                    lpSolve.add_constraint(coefs, lpsolve_constr_types.LE, 0);
                }
            }

            for (int b = 0; b < numB; b++)
            {
                for (int p = 0; p < numP; p++)
                {
                    double[] coefs = new double[1 + numB * numF + numP * numB];
                    for (int b_ = 0; b_ < numB; b_++)
                    {
                        if (b_ == b)
                            for (int f = 0; f < numF; f++)
                                coefs[1 + b_ * numF + f] = a[p, b_, f] * (1 - numB);
                        else
                            for (int f = 0; f < numF; f++)
                                coefs[1 + b_ * numF + f] = a[p, b_, f];
                    }
                    coefs[1 + numB * numF + b * numP + p] = -1;
                    lpSolve.add_constraint(coefs, lpsolve_constr_types.LE, 0);
                }
            }

            //// Add anchor
            //var bottom = new double[1 + numB * numF + numP * numB];
            //bottom[1] = 1;
            //lpSolve.add_constraint(bottom, lpsolve_constr_types.EQ, 1);

            // Set objective
            var he = new double[1 + numB * numF + numP * numB];
            //for (int i = 1 + numB * numF; i < 1 + numB * numF + numP * numB; i++)
            //he[i] = 1; // this weights the error to be minimized for the most abundant proteins. We may want to use relative error. Divide each absolute error by absolute abundance and then all thing are weighted evenly.

            //instead of the preceeding two lines, we added the following loops. then we'll be minimizing the relative error, rather than the absolute error. This will reduce to force implied by high intensity rows and distribute it evenly accross all rows
            for (int b = 0; b < numB; b++)
            {
                for (int p = 0; p < numP; p++)
                {
                    double apb = 0;
                    for (int f = 0; f < numF; f++)
                        apb += a[p, b, f];
                    he[1 + numB * numF + b * numP + p] = 1 / apb;
                }
            }
            lpSolve.set_obj_fn(he);


            for (int i = 1; i <= numB * numF; i++)
                lpSolve.set_lowbo(i, 1);

            var ye = lpSolve.solve();

            var vars = new double[lpSolve.get_Ncolumns()];
            lpSolve.get_variables(vars);
            
            return vars;
        }


        //private static double[] SolveWithLinearEquations(double[,,] a, int numP, int numB, int numF)
        //{
        //    // Need one extra row, to anchor the solutions in one biorep(the first one) to be equal to 1 when summed.
        //    var coefs = new double[numB * numF + 1, numB * numF];

        //    // Populate the coefs matrix
        //    for (int b_ = 0; b_ < numB; b_++)
        //    {
        //        for (int f_ = 0; f_ < numF; f_++)
        //        {
        //            // Working on specific row! This row is the result of taking the gradient with respect to N{b_,f_}

        //            // For b = b_
        //            for (int f = 0; f < numF; f++)
        //                for (int p = 0; p < numP; p++)
        //                    coefs[b_ * numF + f_, b_ * numF + f] += a[p, b_, f] * a[p, b_, f_];

        //            // For b != b_
        //            for (int b = 0; b < numB; b++)
        //                if (b != b_)
        //                    for (int f = 0; f < numF; f++)
        //                        for (int p = 0; p < numP; p++)
        //                            coefs[b_ * numF + f_, b * numF + f] += a[p, b, f] * a[p, b_, f_];

        //        }
        //    }

        //    coefs[numB * numF, 0] = 1;

        //    // Vector of right-hand-sides...
        //    var v = new double[numB * numF + 1];
        //    v[numF * numB] = 1;
        //    var ye = coefs.Solve(v);



        //    //Console.WriteLine(" The Solution:");
        //    for (int b = 0; b < numB; b++)
        //    {
        //        //Console.WriteLine("  Biorep " + b);
        //        for (int f = 0; f < numF; f++)
        //        {
        //            //Console.WriteLine("   " + ye[b * numF + f]);
        //            double someValue = ye[b * numF + f];
        //        }

        //    }
        //    return ye;

        //}
    }
}
        
