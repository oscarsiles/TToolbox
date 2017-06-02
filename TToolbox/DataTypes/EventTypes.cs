using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TToolbox.DataTypes
{
    public class StatusBarMessage
    {
        public string LabelText { get; set; }
        public int? ProgressPercent { get; set; }
    }

    public class ExportCsv
    {
        public object ExportObj { get; set; }
        public AFMtip CurrentTip { get; set; }
        public string ExportPath { get; set; }
        public bool IsFilenameOn { get; set; }
        public bool IsAnalysis { get; set; }
    }

    public class ExportXlsx
    {
        public object ExportObj { get; set; }
        public AFMtip CurrentTip { get; set; }
        public string ExportPath { get; set; }
        public bool IsFilenameOn { get; set; }
        public bool IsAnalysis { get; set; }
    }

    public class FrictionAlpha
    {
        public double Alpha { get; set; }
    }

    public class ForceAdhesion
    {
        public double Adhesion { get; set; }
    }
}
