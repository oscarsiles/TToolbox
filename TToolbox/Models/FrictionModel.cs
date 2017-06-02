using Caliburn.Micro;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TToolbox.DataTypes;
using TToolbox.ViewModels;
using TToolbox.Views;

namespace TToolbox.Models
{
    public class FrictionModel
    {
        private readonly IEventAggregator _eventAggregator;

        private List<FrictionFile> _frictionFiles = new List<FrictionFile>();
        private AFMtip currentTip = new AFMtip();
        private double frictionAdhesion;

        public List<FrictionFile> FrictionFiles
        {
            get { return _frictionFiles; }
            set { _frictionFiles = value; }
        }

        public FrictionModel(List<FrictionFile> frictionFiles, AFMtip tip, IEventAggregator events)
        {
            FrictionFiles = frictionFiles;
            _eventAggregator = events;
            currentTip = tip;
        }

        public bool IsValidFile(string inputPath)
        {
            var ns = new NanoscopeModel();
            return ns.GetFileType(inputPath) == 0; // 0 for force, 1 for friction, 2 for not valid nanoscope file
        }

        public List<FrictionFile> CalculateAllFriction(bool isTrimCI=false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int i = 0;
            int length = FrictionFiles.Count;
            foreach (FrictionFile file in FrictionFiles)
            {
                string outputPath = file.Directory + @"\" + file.Filename;
                // calculate friction
                NanoscopeModel ns = new NanoscopeModel(outputPath);
                ns.Calc_friction(isTrimCI);

                // this section needs improvement...why not just import FrictionFile from NS model?
                FrictionFiles[i].TraceData = ns.TraceData;
                FrictionFiles[i].RetraceData = ns.RetraceData;
                FrictionFiles[i].AvgTraceV = ns.AvgFricTrV;
                FrictionFiles[i].AvgRetraceV = ns.AvgFricRetrV;
                FrictionFiles[i].StdevTraceV = ns.StdFricTrV;
                FrictionFiles[i].StdevRetraceV = ns.StdFricRetrV;
                FrictionFiles[i].AvgFrictionV = ns.AvgFricNewV;
                FrictionFiles[i].StdevFrictionV = ns.StdevFricNewV;
                FrictionFiles[i].TMR = ns.TMR;
                if (currentTip.FrictionAlpha != 0) // if value for alpha, calc in nN as well
                {
                    FrictionFiles[i].AvgTraceNN = ns.AvgFricTrV * currentTip.FrictionAlpha;
                    FrictionFiles[i].AvgRetraceNN = ns.AvgFricRetrV * currentTip.FrictionAlpha;
                    FrictionFiles[i].StdevTraceNN = ns.StdFricTrV * currentTip.FrictionAlpha;
                    FrictionFiles[i].StdevRetraceNN = ns.StdFricRetrV * currentTip.FrictionAlpha;
                    FrictionFiles[i].AvgFrictionNN = ns.AvgFricNewV * currentTip.FrictionAlpha;
                    FrictionFiles[i].StdevFrictionNN = ns.StdevFricNewV * currentTip.FrictionAlpha;
                }


                i++;

                setProgress((int)(100 * i / length));
            }

            sw.Stop();
            Console.WriteLine("Time elapsed: {0:00.00} ms average per friction file", sw.Elapsed.TotalMilliseconds / i); // divide by i to get avg per file

            setProgress(0);
            return FrictionFiles;
        }

        public List<FrictionFile> CalculateCalibration(double frAdhesion)
        {
            frictionAdhesion = frAdhesion;
            for (int i = 0; i < FrictionFiles.Count; i++)
            {
                calcLineAverage(FrictionFiles[i]);

                // DEBUG - save each average to file
                //save_frictionRawDataCsv(FrictionFiles[i], FrictionFiles[i].Directory + @"\debug\avgLines_" + FrictionFiles[i].Filename + ".csv");

                var ratio = FrictionFiles[i].Spl / 512; // 512 samples/line is standard
                var lineAnnotPos = new List<int>(new int[] { (int)(50 * ratio), (int)(200 * ratio), (int)(300 * ratio), (int)(450 * ratio) });


                var graph = new GraphModel();
                var plot = graph.Create_FrictionGraph(FrictionFiles[i], FrictionFiles[i].Filename);
                Execute.OnUIThread(() =>
                    {
                        var win = new WindowManager();
                        var calibVm = new CalibWinViewModel();
                        //calibWin.Owner = base;
                        calibVm.PlotModel = plot;

                        calibVm.Add_Annotation(lineAnnotPos[0], OxyPlot.OxyColors.Blue);
                        calibVm.Add_Annotation(lineAnnotPos[1], OxyPlot.OxyColors.Blue);
                        calibVm.Add_Annotation(lineAnnotPos[2], OxyPlot.OxyColors.Red);
                        calibVm.Add_Annotation(lineAnnotPos[3], OxyPlot.OxyColors.Red);

                        win.ShowDialog(calibVm);
                        lineAnnotPos = calibVm.Get_AnnotationsX();

                        // let's save these
                        FrictionFiles[i].XIntervalValues = lineAnnotPos;
                        //Thread.Sleep(10);

                        // WE NOW HAVE THE X INTERVALS FOR FLAT AND INCLINE, THANKS TO USER
                        // NOW FIND A B C D BY AVERAGING APPROPRIATE RANGES, THEN CALC PARAMETERS FOR μ
                        var tempList = new List<double>();
                        tempList.Add(FrictionFiles[i].AvgRetraceLines.GetRange(lineAnnotPos[0], lineAnnotPos[1] - lineAnnotPos[0]).Average()); // A
                        tempList.Add(FrictionFiles[i].AvgTraceLines.GetRange(lineAnnotPos[0], lineAnnotPos[1] - lineAnnotPos[0]).Average()); // B
                        tempList.Add(FrictionFiles[i].AvgRetraceLines.GetRange(lineAnnotPos[2], lineAnnotPos[3] - lineAnnotPos[2]).Average()); // C
                        tempList.Add(FrictionFiles[i].AvgTraceLines.GetRange(lineAnnotPos[2], lineAnnotPos[3] - lineAnnotPos[2]).Average()); //D

                        FrictionFiles[i].FricCalibParam = tempList;

                        // lets calc μ
                        FrictionFiles[i].Alpha = find_Alpha(FrictionFiles[i]);
                    });
            }

            var alphaTemp = FrictionFiles.OrderByDescending(o => o.Alpha).TakeWhile(p => p.Alpha > 0).Select(q => q.Alpha).ToList(); // filter out any 0 values (order descending, take while >0)

            double avg_alpha = Math.Round(alphaTemp.Mean(), 2); // don't want a hundred decimal places...
            currentTip.FrictionAlpha = avg_alpha;

            _eventAggregator.PublishOnUIThread(new FrictionAlpha { Alpha = avg_alpha });

            return FrictionFiles;
        }

        // misc
        #region lineAverage
        private void calcLineAverage(FrictionFile file)
        {
            // need to average out over all the lines
            file.AvgTraceLines = find_averageLines(file.TraceData);
            file.AvgRetraceLines = find_averageLines(file.RetraceData);
            file.Spl = file.AvgTraceLines.Count;
        }

        private List<double> find_averageLines(List<double[]> linesList)
        {
            List<double> avgLines = new List<double>();

            for (int i = 0; i < linesList[0].Length; i++) // add through all the lines, X=0 -> X=N-1. Sequence matters here
            {
                List<double> toAvg = new List<double>();

                for (int j = 0; j < linesList.Count; j++) // parallel wasn't thread safe...hmm...
                {
                    toAvg.Add(linesList[j][i]);
                }

                avgLines.Add(toAvg.Average());
            }

            return avgLines;
        }
        #endregion

        #region alpha
        private double find_Alpha(FrictionFile calibFile)
        {
            var alpha = 0.0;
            //double muFlat = 0;
            var adhesion = frictionAdhesion * Math.Pow(10, -9); // for now, until we get user to input it! in NEWTONS
            var load = calibFile.LoadV * currentTip.SpringConst * currentTip.DeflSens * Math.Pow(10, -9); // read from file later - CONVERT TO NEWTONS
            var theta = calibFile.ThetaDeg.ToRadians(); // lets convert this to radians

            var W0 = Math.Abs(calibFile.FricCalibParam[3] - calibFile.FricCalibParam[2]) / 2; // |d-c|/2
            var W0flat = Math.Abs(calibFile.FricCalibParam[1] - calibFile.FricCalibParam[0]) / 2; // |b-a|/2 <-- is this correct?
            var d0star = (calibFile.FricCalibParam[3] + calibFile.FricCalibParam[2]) / 2; // (d+c)/2
            var d0flat = (calibFile.FricCalibParam[1] + calibFile.FricCalibParam[0]) / 2; // (b+a)/2
            var d0 = Math.Abs(d0flat - d0star); // |d0flat-d0star|

            // find possible solutions for mu on INCLINE, then validate.
            // if 2 possible, calculate alpha for both, and use the value of alpha that gives the smallest value for |mu_i - mu_i_flat|
            // mu_flat = alpha*W0_flat/(L+A) <- need user to input adhesion between platform and tip // used below

            // lets start with the quadratic eqn and solve it (ax^2+bx+c=0)
            var a = Math.Sin(theta) * (Math.Cos(theta) * load + adhesion);
            var b = -(d0 / W0) * (load + adhesion * Math.Cos(theta));
            var c = load * Math.Sin(theta) * Math.Cos(theta);

            var muList = solve_quadratic(a, b, c); // should we discard negative solutions?

            if (muList == null || muList.Count == 0)
            {
                return 0;
            }
            else if (muList.Count < 2)
            {
                return muList[0];
            }

            var muListTemp = new List<double>();

            foreach (double mu2 in muList)
            {
                // check if between 0 and tan-1(theta)
                if ((0 < mu2) && (mu2 < 1 / Math.Tan(theta)))
                {
                    muListTemp.Add(mu2);
                }
            }

            if (muListTemp.Count == 0) { return alpha = 0; } // no answer?

            var alphaList = find_alphaList(muListTemp, load, adhesion, theta, W0);

            if (alphaList.Count == 1) // we found just one alpha!
            {
                alpha = alphaList[0] * Math.Pow(10, 9); // conversion to nN/V
                //muFlat = alphaList[0] * W0flat / (load + adhesion);
            }
            else if (alphaList.Count == 2) // need to find most appropriate alpha (smallest outcome of |mu_i - mu_i_flat|)
            {
                var muFlatListTemp = new List<double>();
                foreach (double alphaTemp in alphaList)
                {
                    muFlatListTemp.Add(alphaTemp * W0flat / (load + adhesion));
                }

                var value1 = Math.Abs(muListTemp[0] - muFlatListTemp[0]);
                var value2 = Math.Abs(muListTemp[1] - muFlatListTemp[1]);

                if (value1 < value2) { alpha = alphaList[0] * Math.Pow(10, 9); }
                else { alpha = alphaList[1] * Math.Pow(10, 9); }
            }

            return alpha;
        }

        private List<double> find_alphaList(List<double> muList, double load, double adhesion, double theta, double W0)
        {
            var alphaList = new List<double>();

            foreach (double mu in muList)
            {
                var alpha = (mu * (load + adhesion * Math.Cos(theta))) / (W0 * (Math.Pow(Math.Cos(theta), 2) - Math.Pow(mu, 2) * Math.Pow(Math.Sin(theta), 2)));
                alphaList.Add(alpha);
            }

            return alphaList;
        }

        private List<double> solve_quadratic(double a, double b, double c)
        {
            var solutions = new List<double>();
            double sqrtpart = b * b - 4 * a * c;
            double x, x1, x2, img;

            if (sqrtpart > 0) // two real solutions
            {
                x1 = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                x2 = (-b - Math.Sqrt(sqrtpart)) / (2 * a);
                solutions.Add(x1);
                solutions.Add(x2);
            }
            else if (sqrtpart < 0) // two imaginary (don't want this)
            {
                sqrtpart = -sqrtpart;
                x = -b / (2 * a);
                img = Math.Sqrt(sqrtpart) / (2 * a);
                solutions = null;
            }
            else // one (double) solution:
            {
                x = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                solutions.Add(x);
            }

            return solutions;
        }
        #endregion

        private void setProgress(int percent)
        {
            Caliburn.Micro.Execute.OnUIThread(() =>
            {
                _eventAggregator.PublishOnUIThread(new StatusBarMessage { ProgressPercent = percent });
            });
        }
    }
}