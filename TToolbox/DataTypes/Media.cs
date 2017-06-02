using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TToolbox.DataTypes
{
    public class Metal
    {
        // chemical name (lowercase)
        public string Name { get; set; }

        // plasma frequency
        public double Omegap { get; set; }

        // Oscillators' strength
        public double[] F { get; set; }

        // Damping frequency of each oscillator
        public double[] Gamma { get; set; }

        // Resonant frequency of each oscillator
        public double[] Omega { get; set; }

        // Number of resonances
        public int Order { get { return Omega.Length; } }
    }

    public static class Metals
    {
        private static double ehbar = 1.51926751447914e+015;

        public static List<Metal> MetalList
        {
            get
            {
                
                var metalList = new List<Metal>();

                // Gold
                var name = "Au";
                var omegap = 9.03 * ehbar;
                var f = new double[] { 0.760, 0.024, 0.010, 0.071, 0.601, 4.384 };
                var gamma = new double[] { 0.053, 0.241, 0.345, 0.870, 2.494, 2.214 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                var omega = new double[] { 0.000, 0.415, 0.830, 2.969, 4.304, 13.32 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Silver
                name = "Ag";
                omegap = 9.01 * ehbar;
                f = new double[] { 0.845, 0.065, 0.124, 0.011, 0.840, 5.646 };
                gamma = new double[] { 0.048, 3.886, 0.452, 0.065, 0.916, 2.419 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.816, 4.481, 8.185, 9.083, 20.29 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Aluminium
                name = "Al";
                omegap = 14.98 * ehbar;
                f = new double[] { 0.523, 0.227, 0.050, 0.166, 0.030 };
                gamma = new double[] { 0.047, 0.333, 0.312, 1.351, 3.382 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.162, 1.544, 1.808, 3.473 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Copper
                name = "Cu";
                omegap = 10.83 * ehbar;
                f = new double[] { 0.575, 0.061, 0.104, 0.723, 0.638 };
                gamma = new double[] { 0.030, 0.378, 1.056, 3.213, 4.305 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.291, 2.957, 5.300, 11.18 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Chromium
                name = "Cr";
                omegap = 10.75 * ehbar;
                f = new double[] { 0.168, 0.151, 0.150, 1.149, 0.825 };
                gamma = new double[] { 0.047, 3.175, 1.305, 2.676, 1.335 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.121, 0.543, 1.970, 8.775 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Nickel
                name = "Ni";
                omegap = 15.92 * ehbar;
                f = new double[] { 0.096, 0.100, 0.135, 0.106, 0.729 };
                gamma = new double[] { 0.048, 4.511, 1.334, 2.178, 6.292 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.174, 0.582, 1.597, 6.089 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Tungsten
                name = "W";
                omegap = 13.22 * ehbar;
                f = new double[] { 0.206, 0.054, 0.166, 0.706, 2.590 };
                gamma = new double[] { 0.064, 0.530, 1.281, 3.332, 5.836 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 1.004, 1.917, 3.580, 7.498 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Titanium
                name = "Ti";
                omegap = 7.29 * ehbar;
                f = new double[] { 0.148, 0.899, 0.393, 0.187, 0.001 };
                gamma = new double[] { 0.082, 2.276, 2.518, 1.663, 1.762 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.777, 1.545, 2.509, 1.943 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Beryllium
                name = "Be";
                omegap = 18.51 * ehbar;
                f = new double[] { 0.084, 0.031, 0.140, 0.530, 0.130 };
                gamma = new double[] { 0.035, 1.664, 3.395, 4.454, 1.802 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.100, 1.032, 3.183, 4.604 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // Palladium
                name = "Pd";
                omegap = 9.59 * ehbar;
                f = new double[] { 0.333, 0.191, 0.659, 0.547, 3.576 };
                gamma = new double[] { 0.080, 0.517, 1.838, 3.668, 8.517 };
                Parallel.For(0, gamma.Length, i => gamma[i] *= ehbar);
                omega = new double[] { 0.000, 0.780, 1.314, 3.141, 9.249 };
                Parallel.For(0, omega.Length, i => omega[i] *= ehbar);

                metalList.Add(new Metal
                {
                    Name = name,
                    Omegap = omegap,
                    F = f,
                    Gamma = gamma,
                    Omega = omega
                });

                // water
                metalList.Add(new Metal { Name = "H2O" });

                return metalList;
            }
        }
    }

    public class NonMetal
    {
        // chemical name (lowercase)
        public string Name { get; set; }

        // static dielectric constant
        public double StaticDielec { get; set; }

        // oscillator strength IR
        public double Cir { get; set; }

        // oscillation frequency IR
        public double Omegair { get; set; }

        // oscillator strength UV
        public double Cuv { get; set; }

        // oscillation frequency UV
        public double Omegauv { get; set; }
    }

    public static class NonMetals
    {
        public static double radsToHz = 0.1591549; // convert to Hz from rads

        public static List<NonMetal> NonMetalList
        {
            get
            {
                var nonMetalList = new List<NonMetal>();

                // dodecane
                //var name = "dodecane";
                //var cir = 0.0244;
                //var omegair = 5.52E14; // in rad/s
                //var cuv = 0.9916;
                //var omegauv = 1.88E16; // in rad/s
                //var staticDielec = 2.016;
                var name = "sam"; // from abbott's paper
                var cir = 0.025;
                var omegair = 8.8E13; // in rad/s
                //var cuv = 1.103;
                //var omegauv = 2.9E16; // in rad/s
                var cuv = 1.5;
                var omegauv = 1.9E16;
                var staticDielec = 2.1;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });

                // methanol
                name = "methanol";
                cir = 3.955;
                omegair = 3.52E14; // in rad/s
                cuv = 0.7448;
                omegauv = 1.91E16; // in rad/s
                staticDielec = 33.0;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });

                // ethanol
                name = "ethanol";
                cir = 2.370;
                omegair = 2.59E14; // in rad/s
                cuv = 0.8292;
                omegauv = 1.90E16; // in rad/s
                staticDielec = 25.0;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });

                // decane
                name = "decane";
                cir = 0.028;
                omegair = 5.51E14; // in rad/s
                cuv = 0.9630;
                omegauv = 1.85E16; // in rad/s
                staticDielec = 1.991;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });


                // dodecane
                name = "dodecane";
                cir = 0.0244;
                omegair = 5.52E14; // in rad/s
                cuv = 0.9916;
                omegauv = 1.88E16; // in rad/s
                staticDielec = 2.016;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });

                // perfluorodecalin
                name = "perfluorodecalin";
                cir = 0.0113;
                omegair = 2.31E14; // in rad/s
                cuv = 0.7156;
                omegauv = 2.55E16; // in rad/s
                staticDielec = 1.727;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });

                // heptane
                name = "heptane";
                cir = 0.0186;
                omegair = 2.356E14;
                cuv = 0.579;
                omegauv = 1.88E16;
                staticDielec = 1.900;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });

                // SiO2
                name = "sio2";
                cir = 1.71;
                omegair = 1.88E14;
                cuv = 1.098;
                omegauv = 2.01E16;
                staticDielec = 3.81;

                nonMetalList.Add(new NonMetal
                {
                    Name = name,
                    Cir = cir,
                    Omegair = omegair,
                    Cuv = cuv,
                    Omegauv = omegauv,
                    StaticDielec = staticDielec
                });

                return nonMetalList;
            }
        }
            
    }
}
