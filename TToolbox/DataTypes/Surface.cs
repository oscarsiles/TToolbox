namespace TToolbox
{
    public class Surface
    {
        public string Name { get; set; }

        public string EndGroup { get; set; }

        public string Type { get; set; }

        public double Ei { get; set; }

        public double Ej { get; set; }

        public double DielectricBulk { get; set; }

        public double RefractiveBulk { get; set; }

        public double DielectricChain { get; set; }

        public double RefractiveChain { get; set; }

        public double DielectricEndGroup { get; set; }

        public double RefractiveEndGroup { get; set; }

        public double VolumeEndGroup { get; set; } // cm^3/mol

        public double Thickness { get; set; }
    }
}