using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TToolbox.DataTypes
{
    public class ModelSystem
    {
        public string Name { get; set; }

        public List<int> PdbList { get; set; }

        public int NoSolidMedia { get; set; }

        public List<Surface> SolidMedia { get; set; }

        public List<Solvent> LiquidMedia { get; set; }

        // list in case of mixture
        public List<double> VolFrac { get; set; }

        public List<double> Wad { get; set; }

        public List<double> DG { get; set; }

        public double Temp = 298; // room temp for all calculations
    }
}
