using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using System.Numerics;
using TToolbox.DataTypes;

namespace TToolbox.Models
{
    public static class Dielectric
    {
        // N.B. Gamma and omega values are in eV, while f is adimensional.
        private static double twopic = 1.883651567308853e+09; //twopic=2*pi*c where c is speed of light
        private static double ehbar = 1.51926751447914e+015;
        private static double invsqrt2 = 0.707106781186547;  // 1/sqrt(2)
        private static double c = 2.9979E8;
        private static double omegalight = 0;

        public static double LifshitzImaginaryFreqIntegral(string phase1, string phase2, string phase3, double startFreq, double endFreq, double initialEstimate=1E10)
        {

            var integral = Integrate.OnClosedInterval(ImaginaryFreqIntegral(phase1, phase2, phase3), startFreq, endFreq, initialEstimate);

            return integral;
        }

        public static Func<double, double> ImaginaryFreqIntegral(string str1, string str2, string str3)
        {
            // IMPORTANT - need to select appropriate function (metal/nonmetal)!
            Func<double, double> imDielecFunc1;
            Func<double, double> imDielecFunc2;
            Func<double, double> imDielecFunc3;

            var metalsList = Metals.MetalList; // expensive to keep calling the class over and over
            metalsList.Add(new Metal { Name = "h2o" }); // hack to fix wrong function selection

            if (metalsList.Exists(x => x.Name.ToLower() == str1.ToLower())) // is it a metal?
            {
                imDielecFunc1 = (x) => kramersKronigImFreq(x, str1);
            }
            else // its a nonmetal
            {
                imDielecFunc1 = (x) => imFreqDielectricCalc_nonmetal(x, str1);
            }

            if (metalsList.Exists(x => x.Name.ToLower() == str2.ToLower())) // is it a metal?
            {
                imDielecFunc2 = (x) => kramersKronigImFreq(x, str2);
            }
            else // its a nonmetal
            {
                imDielecFunc2 = (x) => imFreqDielectricCalc_nonmetal(x, str2);
            }

            if (metalsList.Exists(x => x.Name.ToLower() == str3.ToLower())) // is it a metal?
            {
                imDielecFunc3 = (x) => kramersKronigImFreq(x, str3);
            }
            else // its a nonmetal
            {
                imDielecFunc3 = (x) => imFreqDielectricCalc_nonmetal(x, str3);
            }

            Func<double, double> func = (x) => ((imDielecFunc1(x) - imDielecFunc3(x)) / (imDielecFunc1(x) + imDielecFunc3(x))) *
                                                ((imDielecFunc2(x) - imDielecFunc3(x)) / (imDielecFunc2(x) + imDielecFunc3(x)));

            //Func<double, double> func = (x) => ((kramersKronigImFreq(x, str1) - kramersKronigImFreq(x, str3)) / (kramersKronigImFreq(x, str1) + kramersKronigImFreq(x, str3))) *
            //                                    ((kramersKronigImFreq(x, str2) - kramersKronigImFreq(x, str3)) / (kramersKronigImFreq(x, str2) + kramersKronigImFreq(x, str3)));

            //var test = func(1E25);

            return func;
        }

        #region metals
        /* based off of program by:
              Bora Ung
              Ecole Polytechnique de Montreal
              Dept. Engineering physics
              2500 Chemin de Polytechnique
              Montreal, Canada
              H3T 1J4
              boraung@gmail.com

        Ref: B. Ung and Y. Sheng, Interference of surface waves in a metallic nanoslit, Optics Express (2007)
        */

        public static Tuple<double, double> Dielectric_Calc(double lambda, string materialstr)
        {
            var freq = Math.Pow(lambda, -1); // convert to frequency
            //var freq = c / lambda;
            var metalsList = Metals.MetalList;
            var nonMetalsList = NonMetals.NonMetalList;
            if (materialstr.ToLower() == "h2o")
            {
                return dielectricCalc_water(freq);
            }
            else if (metalsList.Exists(x => x.Name.ToLower() == materialstr.ToLower())) // is metal?
            {
                return dielectricCalc_metal(freq, materialstr);
            }
            else
            {
                if (materialstr == "h2o")
                {
                    return new Tuple<double, double>(1, 0);
                }
                else
                {
                    try
                    {
                        var nonmetal = NonMetals.NonMetalList.Find(x => x.Name.ToLower() == materialstr.ToLower());
                        return new Tuple<double, double>(nonmetal.StaticDielec, 0);
                    }
                    catch (Exception)
                    {
                        return new Tuple<double, double>(0, 0);
                        throw;
                    }
                }
                //return new Tuple<double, double>(imFreqDielectricCalc_nonmetal(freq, materialstr), 0);
            }
        }

        public static double kramersKronigImFreq(double imFreq, string metalstr)
        {
            var factor = 1.8837E18;
            //var lambda = factor / 1E12;
            //var bla = Dielectric_Calc(lambda * 1E-9, metalstr).Item2;

            Func<double, double> func = (x) => (x * Dielectric_Calc((factor / x) * 1E-9, metalstr).Item2) / (Math.Pow(x, 2) + Math.Pow(imFreq, 2)); // convert to nm

            var integral = Integrate.OnClosedInterval(func, 1E12, 1E20/*, 1E3*/); // 1E12 - 1E17 accurate range??
            var imDielectric = 1 + (2 / Math.PI) * integral; 

            return imDielectric;
        }

        private static Tuple<double, double> dielectricCalc_metal(double freq, string metalstr)
        {
            //omegalight = twopic/lambda;
            omegalight = twopic * freq;


            //var metals = new Metals();
            var metal = new Metal();
            try
            {
                metal = Metals.MetalList.Find(x => x.Name.ToLower() == metalstr.ToLower());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }


            if (omegalight == 0) // return static dielectric
            {
                return new Tuple<double, double>(1, 0);
            }

            //
            // Drude model (intraband effects in metals)
            //
            // lets make a complex-input friendly function..
            var complex1 = new Complex(metal.F[0] * Math.Pow(metal.Omegap, 2), 0);
            var complex2 = Complex.Multiply(omegalight, omegalight);
            var complex3 = metal.Gamma[0] * omegalight * Complex.ImaginaryOne;
            var epsilon_D = Complex.Subtract(new Complex(1, 0), Complex.Divide(complex1, Complex.Add(complex2, complex3)));


            //
            // Lorentz model (interband effects)
            //

            //epsilon_L = zeros(size(lambda));

            //Lorentzian contributions
            var epsilon_L = dielectric_lorentz(metal);

            var epsilon = Complex.Add(epsilon_D, epsilon_L);
            return new Tuple<double, double>(epsilon.Real, epsilon.Imaginary);
        }

        private static Tuple<double, double> dielectricCalc_water(double freq)
        {
            //omegalight = twopic / lambda;
            omegalight = twopic * freq;

            if (omegalight == 0 || double.IsNaN(omegalight))
            {
                return new Tuple<double, double>(80.1, 0);
            }

            // using 2-pole Debye model
            //Debye parameters (microwave frequencies)
            var a = new double[] { 74.65, 2.988 };
            var tauj = new double[] { 8.30e-12, 5.91e-14 }; // [sec]
            var tauf = new double[] { 1.09e-13, 8.34e-15 }; // [sec]
            var nu = new double[] { 0, -0.5 };
            var debye_order = a.Length;

            // Lorentz parameters(infrared and optical frequencies)
            var omegap = ehbar; // "virtual" plasma frequency
            var f = new double[] { 0, 1.0745e-05, 3.1155e-03, 1.6985e-04, 1.1795e-02, 1.7504e+02 };
            var gamma = new double[] { 0, 0.0046865, 0.059371, 0.0040546, 0.037650, 7.66167 };
            Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
            var omega = new double[] { 0, 0.013691, 0.069113, 0.21523, 0.40743, 15.1390 };
            Parallel.For(0, omega.Length, i => omega[i] *= ehbar);
            var order = omega.Length;

            // fits into lorentz method
            var h2o_metal = new Metal {
                Omegap = omegap,
                F = f,
                Gamma = gamma,
                Omega = omega };

            // lets make it complex-input friendly...
            var epsilon_D = new Complex(1, 0);
            for (int i = 0; i < debye_order; i++)
            {
                var complex1 = Complex.Subtract(1, Complex.Pow(Complex.ImaginaryOne * tauj[i] * omegalight, 1 - nu[i]));
                var complex2 = Complex.Subtract(1, Complex.ImaginaryOne * tauf[i] * omegalight);
                var complex3 = Complex.Divide(a[i], Complex.Multiply(complex1, complex2));

                epsilon_D = Complex.Add(epsilon_D, complex3);
            }

            var epsilon_L = dielectric_lorentz(h2o_metal);

            var epsilon = Complex.Add(epsilon_D, epsilon_L);

            return new Tuple<double, double>(epsilon.Real, epsilon.Imaginary);
        }

        private static Complex dielectric_lorentz(Metal metal)
        {
            // lets use complex inputs!

            var epsilon_L = new Complex(0, 0);
            for (int i = 1; i < metal.Order; i++) // starting at i=1 because 0th order done with drude model
            {
                var complex1 = new Complex(metal.F[i] * Math.Pow(metal.Omegap, 2), 0);
                var complex2 = Math.Pow(metal.Omega[i], 2) - Complex.Multiply(omegalight, omegalight);
                var complex3 = metal.Gamma[i] * omegalight * Complex.ImaginaryOne;
                var complex4 = Complex.Divide(complex1, Complex.Subtract(complex2, complex3));

                epsilon_L = Complex.Add(epsilon_L, complex4);
            }

            return epsilon_L;
        }
        #endregion

        #region nonmetals
        public static double imFreqDielectricCalc_nonmetal(double freq, string nonmetalstr)
        {
            //omegalight = twopic / lambda;
            omegalight = freq;

            if (nonmetalstr == "air")
            {
                return 1;
            }

            var nonmetal = new NonMetal();
            try
            {
                nonmetal = NonMetals.NonMetalList.Find(x => x.Name.ToLower() == nonmetalstr.ToLower());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }

            double imFreqDielectric = 0;

            if (omegalight == 0)
            {
                imFreqDielectric = nonmetal.StaticDielec;
            }
            else if (omegalight > 0)
            {
                imFreqDielectric = 1 + (nonmetal.Cir / (1 + Math.Pow((omegalight / nonmetal.Omegair), 2)))
                                     + (nonmetal.Cuv / (1 + Math.Pow((omegalight / nonmetal.Omegauv), 2)));
            }

            return imFreqDielectric; // replace this
        }
        #endregion
    }
}
