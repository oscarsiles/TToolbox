namespace TToolbox.DataTypes
{
    public class Solvent
    {
        public string Name { get; set; }

        public int PDB { get; set; }

        public double Dielectric { get; set; }

        public double Refractive { get; set; }

        public double Wad3 { get; set; }

        public double Wad5 { get; set; }

        public double dG { get; set; } // in J!
    }
}