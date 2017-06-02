using System.Collections.Generic;

namespace TToolbox
{
    public class ForceFile
    {
        public string FullPath { get; set; }
        public string Directory { get; set; }
        public string Filename { get; set; }
        public int Index { get; set; }
        public List<double> B { get; set; }
        public List<double> Va { get; set; }
        public List<double> Vr { get; set; }
        public double? DeflSens { get; set; }
        public double? PullOffV { get; set; }
        public double? PullOffNM { get; set; }
        public double? PullOffNN { get; set; }
        public double? MaxDefl { get; set; }
    }
}