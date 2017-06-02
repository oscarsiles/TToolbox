using Caliburn.Micro;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TToolbox.DataTypes;

namespace TToolbox.Models
{
    internal class ModellingModel
    {
        private readonly IEventAggregator _eventAggregator;
        private List<Surface> surfaceList = new List<Surface>();
        public List<Solvent> SolventList = new List<Solvent>();

        // constants
        private readonly double T = 298;

        private const double k_B = 1.38e-23;
        private const double h = 6.63e-34;
        private const double d_0 = 0.165e-9;
        private const double v_e = 3.3e15;
        private const double c_max = 300;

        public ModellingModel(List<Surface> surfaces, List<Solvent> solvents, IEventAggregator events)
        {
            // initialize components
            this.surfaceList = surfaces;
            this.SolventList = solvents;
            _eventAggregator = events;
        }

        public ModelSystem CalculateAll(string outputpt_path, int noSolidMedia=2)
        {
            
            int finalNumber = SolventList.Count;

            var _surfaceList = surfaceList;
            var _solventList = new List<Solvent>();
            var _wadList = new List<double>();
            var _DGList = new List<double>();
            var _pdbList = new List<int>();
            var modelName = "";


            // find way to do with PLINQ (to keep order!)
            int j = 0;
            Parallel.ForEach(SolventList, sol =>
            {
                var wad = 0.0;
                if (noSolidMedia == 2)
                {
                    modelName = String.Format("{0}|{1}", surfaceList[0].Name, surfaceList[1].Name);
                    wad = Wad3Calc(surfaceList[0], surfaceList[1], sol);
                    sol.Wad3 = wad;
                }
                else if (noSolidMedia == 4)
                {
                    modelName = String.Format("{0}+{1}({2}nm)|{3}+{4}({5}nm)", surfaceList[0].Name, surfaceList[2].Name, surfaceList[2].Thickness, surfaceList[1].Name, surfaceList[3].Name, surfaceList[3].Thickness);
                    wad = Wad5Calc(surfaceList[0], surfaceList[1], surfaceList[2], surfaceList[3], sol);
                    sol.Wad5 = wad;
                }

                // only dG of innermost surfaces!
                Surface dgSur1 = null;
                Surface dgSur2 = null;
                if (noSolidMedia == 2)
                {
                    dgSur1 = surfaceList[0];
                    dgSur2 = surfaceList[1];
                }
                else if (noSolidMedia == 4)
                {
                    dgSur1 = surfaceList[2];
                    dgSur2 = surfaceList[3];
                }
                var k = KCalc(dgSur1, dgSur2);
                var dGpath = outputpt_path + @"\" + dgSur1.Name + @"\" + dgSur2.Name + @"\single\";
                sol.dG = DGCalc(k, dGpath, sol.Name);

                lock (this)
                {
                    _solventList.Add(sol);
                    _wadList.Add(wad);
                    _DGList.Add(sol.dG);
                    _pdbList.Add(sol.PDB);
                }

                //update progressbar
                setProgress((int)(100 * j / finalNumber));
                Interlocked.Increment(ref j);
            });


            var orderedPdb = _pdbList.OrderBy(x => x).ToList();
            var orderedSolvent = _solventList.OrderBy(x => _pdbList).ToList();
            
            var modelSystem = new ModelSystem() 
            {
                Name = modelName,
                PdbList = _pdbList,
                SolidMedia = _surfaceList,
                LiquidMedia = _solventList,
                Wad = _wadList,
                DG = _DGList
            };

            setProgress(0);

            return modelSystem;
        }

        public double Wad3Calc(Surface sur1, Surface sur2, Solvent sol)
        {
            // need to check input parameters
            var a132 = calcA132(sur1.DielectricBulk, sur2.DielectricBulk, sol.Dielectric, sur1.RefractiveBulk, sur2.RefractiveBulk, sol.Refractive, T);
            var wad3 = a132 / (12 * Math.PI * Math.Pow(d_0, 2));
            return wad3;
        }

        public double Wad5Calc(Surface sur1, Surface sur2, Surface sur3, Surface sur4, Solvent sol)
        {
            // set up hamaker constants
            var A232d = calcA132(sur3.DielectricBulk, sur4.DielectricBulk, sol.Dielectric, sur3.RefractiveBulk, sur4.RefractiveBulk, sol.Refractive, T);
            var A121 = calcA132(sur1.DielectricBulk, sur1.DielectricBulk, sur3.DielectricBulk, sur1.RefractiveBulk, sur1.RefractiveBulk, sur3.RefractiveBulk, T);
            var A32d3 = calcA132(sol.Dielectric, sol.Dielectric, sur4.DielectricBulk, sol.Refractive, sol.Refractive, sur4.RefractiveBulk, T);
            var A1d2d1d = calcA132(sur2.DielectricBulk, sur2.DielectricBulk, sur4.DielectricBulk, sur2.RefractiveBulk, sur2.RefractiveBulk, sur4.RefractiveBulk, T);
            var A323 = calcA132(sol.Dielectric, sol.Dielectric, sur3.DielectricBulk, sol.Refractive, sol.Refractive, sur3.RefractiveBulk, T);

            var T1 = sur3.Thickness * 1e-9;
            var T2 = sur4.Thickness * 1e-9;

            // set up our function
            Func<double, double> wad5ForceFunc = x => ((1 / (6 * Math.PI)) * (A232d / (Math.Pow(x, 3)) - (Math.Sqrt(A121 * A32d3)) / (Math.Pow(x + T1, 3)) - (Math.Sqrt(A1d2d1d * A323)) / (Math.Pow(x + T2, 3)) + (Math.Sqrt(A1d2d1d * A121)) / (Math.Pow(x + T1 + T2, 3)))); // x = D
            var wad5 = Integrate.OnClosedInterval(wad5ForceFunc, d_0, 1); // setting upper limit to 1(meter) stops weird results, as function not normalized to y=0 it seems

            return wad5;
        }

        public double DGCalc(double k, string dir, string name)
        {
            DataSet ds = new DataSet();
            CSV csv = new CSV();

            string filename = "phaset_" + name + ".csv";
            string filePath = dir + filename;

            // exit if file doesn't exist
            if (!File.Exists(filePath))
            {
                return 0;
            }

            try
            {
                // lets import the data from phasetransfer
                ds = csv.ReadCSV(filePath, false); // CsvReader is 35x faster than using OdbcConnection! Don't want to include headers, as duplicate names exist

                DataTable table = ds.Tables[0]; // first table, as only one table exists in the dataset

                // header is included in dataset, so offset rows by +1! (since duplicate header names exist, which CsvReader doesn't like)
                var dG_si = Convert.ToDouble(table.Rows[223].ItemArray[18]) * 1000; // convert string to number, x1000 to convert into J from kJ
                var dG_sj = Convert.ToDouble(table.Rows[224].ItemArray[18]) * 1000;
                var theta = Convert.ToDouble(table.Rows[223].ItemArray[16]);

                var dG_c = -2 * 8.314 * T * Math.Log((Math.Sqrt(1 + 8 * theta) - 1) / (4 * theta));
                var dG_b = 2 * 8.314 * T * Math.Log((Math.Sqrt(1 + 8 * k * theta) - 1) / (4 * k * theta));

                // put it all together
                var dG = dG_b + dG_c - dG_si - dG_sj + 8.314 * T * Math.Log(c_max);
                return dG;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }

        public double KCalc(Surface sur1, Surface sur2)
        {
            var e_i = sur1.Ei;
            var e_j = sur2.Ej;

            // lets do our preliminary calculations
            var e_ij = calcEkl(e_i, e_j, T);
            var k_ij = calcKkl(e_ij, T);
            var e_ii = calcEkl(e_i, e_i, T);
            var k_ii = calcKkl(e_ii, T);
            var e_jj = calcEkl(e_j, e_j, T);
            var k_jj = calcKkl(e_jj, T);
            var k = (2 * k_ij + k_ii + k_jj) / 4;

            return k;
        }

        // CUSTOM METHODS

        private double calcA132(double e1, double e2, double e3, double n1, double n2, double n3, double T)
        {
            double a;

            if (e1 == e2 && n1 == n2) // same surfaces, can use reduced form
            {
                a = ((3 * k_B * T) / 4) * Math.Pow((e1 - e3) / (e1 + e3), 2) + ((3 * h * v_e) / (16 * Math.Sqrt(2))) * (Math.Pow(Math.Pow(n1, 2) - Math.Pow(n3, 2), 2) / Math.Pow(Math.Pow(n1, 2) + Math.Pow(n3, 2), 1.5));
            }
            else
            {
                a = ((3 * k_B * T) / 4) * ((e1 - e3) / (e1 + e3)) * ((e2 - e3) / (e2 + e3)) + ((3 * h * v_e) / (8 * Math.Sqrt(2))) * (((Math.Pow(n1, 2) - Math.Pow(n3, 2)) * (Math.Pow(n2, 2) - Math.Pow(n3, 2))) / ((Math.Sqrt(Math.Pow(n1, 2) + Math.Pow(n3, 2)) * Math.Sqrt(Math.Pow(n2, 2) + Math.Pow(n3, 2))) * (Math.Sqrt(Math.Pow(n1, 2) + Math.Pow(n3, 2)) + Math.Sqrt(Math.Pow(n2, 2) + Math.Pow(n3, 2)))));
            }

            return a;
        }

        private double calcEkl(double e_k, double e_l, double T)
        {
            double e_kl;
            if (e_k == e_l) // special case if both the same
            {
                e_kl = (e_k * e_l) - 5.6; // kJ/mol
            }
            else
            {
                e_kl = (e_k * e_l) / (1 + Math.Exp((e_k * e_l * 1000) / (8.314 * T))) - 5.6; // kJ/mol
            }

            return e_kl;
        }

        private double calcKkl(double e_kl, double T)
        {
            double k_kl = 0.5 * Math.Exp((-1 * e_kl * 1000) / (8.314 * T)); // normalize from kJ/mol to J/mol
            return k_kl;
        }

        private void setProgress(int percent)
        {
            Caliburn.Micro.Execute.OnUIThread(() =>
            {
                _eventAggregator.PublishOnUIThread(new StatusBarMessage { ProgressPercent = percent });
            });
        }

        private double integrate(Func<double, double> f, double intPointOne, double intPointTwo, int N_steps)
        {
            double integral = 0;
            double i = intPointOne;
            double stepSize = (intPointTwo - intPointOne) / N_steps;
            do
            {
                integral += f(i) * stepSize;
                i += stepSize;
            }
            while (i <= intPointTwo);
            return integral;
        }
    }
}