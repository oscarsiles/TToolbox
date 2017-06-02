using System.Collections.Generic;

namespace TToolbox
{
    public class FrictionFile
    {
        public string FullPath { get; set; }

        public string Directory { get; set; }

        public string Filename { get; set; }

        public double LoadV { get; set; }

        public double DeflSens { get; set; }

        public List<double[]> TraceData { get; set; }

        public List<double[]> RetraceData { get; set; }

        // fixed method
        public double[] TraceLineAvg { get; set; }

        public double[] RetraceLineAvg { get; set; }

        public double[] LineAvg { get; set; }

        public double[] LineAvgFricV { get; set; }

        public List<double> TMR { get; set; }

        public double AvgFrictionV { get; set; }

        public double StdevFrictionV { get; set; }

        public double AvgFrictionNN { get; set; }

        public double StdevFrictionNN { get; set; }

        public double AvgTraceV { get; set; }

        public double AvgRetraceV { get; set; }

        public double StdevTraceV { get; set; }

        public double StdevRetraceV { get; set; }

        public double AvgTraceNN { get; set; }

        public double AvgRetraceNN { get; set; }

        public double StdevTraceNN { get; set; }

        public double StdevRetraceNN { get; set; }

        // calibration stuff

        public double ThetaDeg = 54.44; // hardcoded for now, as we only use TGF11

        public double Spl { get; set; } // samples per line

        public List<double> AvgTraceLines { get; set; }

        public List<double> AvgRetraceLines { get; set; }

        public List<int> XIntervalValues { get; set; } // these are the marker positions set by user

        public List<double> FricCalibParam { get; set; } // A, B, C, D -> in order

        public double Alpha { get; set; }

        public double Mu_flat { get; set; }

        public double ExecutionTimeMS { get; set; }
    }
}