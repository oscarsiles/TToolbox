using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TToolbox.Models
{
    internal class GraphModel
    {
        //public PlotModel plotModel { get; private set; }

        public GraphModel()
        {
        }

        public void Create_PullOffGraph(string output, List<double> x, List<double> y1, List<double> y2 = null, string title = "empty")
        {
            var plotModel = new PlotModel();
            plotModel.Title = title;

            // create lineSeries and change colors and stroke thickness
            var va = create_LineSeries(x, y1, "extend");
            va.Color = OxyColors.Blue;
            va.StrokeThickness = 0.5;
            va.Smooth = false;

            var vr = create_LineSeries(x, y2, "retract");
            vr.Color = OxyColors.Red;
            vr.StrokeThickness = 0.5;
            vr.Smooth = false;

            // set axis details
            var linearAxisX = new OxyPlot.Axes.LinearAxis();
            linearAxisX.Position = AxisPosition.Bottom;
            linearAxisX.Title = "z";
            linearAxisX.Unit = "nm";
            plotModel.Axes.Add(linearAxisX);

            var linearAxisY = new OxyPlot.Axes.LinearAxis();
            linearAxisY.Position = AxisPosition.Left;
            linearAxisY.Title = "Detector Voltage";
            linearAxisY.Unit = "V";
            linearAxisY.MinimumPadding = 0.1;
            plotModel.Axes.Add(linearAxisY);

            plotModel.Series.Add(va);
            plotModel.Series.Add(vr);
            saveGraph(plotModel, output);
        }

        public PlotModel Create_ForceHistogram()
        {
            return null;
        }

        public PlotModel Create_FrictionGraph(FrictionFile file, string title = "Friction")
        {
            var plotModel = new PlotModel();
            plotModel.Title = title;
            plotModel.Subtitle = "Drag markers into appropriate positions";
            plotModel.IsLegendVisible = false;

            // lets populate our x axis
            var xList = Enumerable.Range(0, file.AvgTraceLines.Count).Select(i => (double)i).ToList();

            // create lineseries
            var trace = create_LineSeries(xList, file.AvgTraceLines, "trace");
            trace.Color = OxyColors.Blue;
            trace.StrokeThickness = 0.5;
            trace.Smooth = false;

            var retrace = create_LineSeries(xList, file.AvgRetraceLines, "retrace");
            retrace.Color = OxyColors.Red;
            retrace.StrokeThickness = 0.5;
            retrace.Smooth = false;

            // set axis details
            var linearAxisX = new OxyPlot.Axes.LinearAxis();
            linearAxisX.Position = AxisPosition.Bottom;
            //linearAxisX.Title = "Pixel";
            //linearAxisX.Unit = "";
            linearAxisX.IsZoomEnabled = false;
            linearAxisX.IsPanEnabled = false;
            plotModel.Axes.Add(linearAxisX);

            var linearAxisY = new OxyPlot.Axes.LinearAxis();
            linearAxisY.Position = AxisPosition.Left;
            //linearAxisY.Title = "Friction Force";
            //linearAxisY.Unit = "V";
            linearAxisY.MinimumPadding = 0.1;
            linearAxisY.IsZoomEnabled = false;
            linearAxisY.IsPanEnabled = false;
            plotModel.Axes.Add(linearAxisY);

            // add line annotations in CalibWindow class

            plotModel.Series.Add(trace);
            plotModel.Series.Add(retrace);

            //saveGraph(plotModel, @"C:\test\friction.png");
            return plotModel;
        }

        public PlotModel Create_FrictionLoadGraph(List<FrictionFile> frictionFiles, string title = "Friction vs Load")
        {
            // check if files list is empty
            if (frictionFiles.Count == 0)
                return null;

            var xyList = new List<Tuple<double, double>>();

            bool isNN = false;

            // check whether nN or V
            if (frictionFiles[0].AvgFrictionNN != 0 /*&& frictionFiles[0].AvgFricNewNN != null*/) // in case we make them nullable later
                isNN = true;

            // lets get the data we want (we will check for duplicates after merging)
            // ignore errors for now!
            foreach (var file in frictionFiles)
            {
                var x = file.LoadV;
                var y = 0.0;

                if (isNN)
                {
                    y = file.AvgFrictionNN;
                }
                else
                {
                    y = file.AvgFrictionV;
                }

                xyList.Add(Tuple.Create(x, y));
            }

            // remove duplicates and fix order
            xyList = xyList.GroupBy(x => x.Item1, (x, y) => Tuple.Create(x, y.Average(i => i.Item2)))
                           .OrderBy(i => i.Item1)
                           .ToList();

            var plotModel = new PlotModel();
            plotModel.Title = title;
            //plotModel.Subtitle = "Drag markers into appropriate positions";
            plotModel.IsLegendVisible = false;

            // create lineseries
            var frictionLoadSeries = create_LineSeries(xyList.Select(x => x.Item1).ToList(), xyList.Select(y => y.Item2).ToList(), "frictionLoad");
            frictionLoadSeries.Color = OxyColors.Blue;
            frictionLoadSeries.StrokeThickness = 1.5;
            frictionLoadSeries.Smooth = false;
            frictionLoadSeries.MarkerType = MarkerType.Diamond;

            // set axis details
            var linearAxisX = new LinearAxis();
            linearAxisX.Position = AxisPosition.Bottom;
            linearAxisX.Title = "Load";
            linearAxisX.Unit = "V";
            linearAxisX.IsZoomEnabled = false;
            linearAxisX.IsPanEnabled = false;
            plotModel.Axes.Add(linearAxisX);

            var linearAxisY = new LinearAxis();
            linearAxisY.Position = AxisPosition.Left;
            linearAxisY.Title = "Friction Force";
            if (isNN)
                linearAxisY.Unit = "nN";
            else
                linearAxisY.Unit = "V";
            linearAxisY.MinimumPadding = 0.1;
            linearAxisY.IsZoomEnabled = false;
            linearAxisY.IsPanEnabled = false;
            plotModel.Axes.Add(linearAxisY);

            // add line annotations in CalibWindow class

            plotModel.Series.Add(frictionLoadSeries);

            saveGraph(plotModel, @"C:\test\friction.png");

            return plotModel;
        }

        private LineSeries create_LineSeries(List<double> x, List<double> y, string seriesName = null)
        {
            var lineSeries = new OxyPlot.Series.LineSeries();
            lineSeries.Title = seriesName;

            for (int i = 0; i < x.Count; i++)
            {
                lineSeries.Points.Add(new DataPoint(x[i], y[i]));
            }

            return lineSeries;
        }

        private void saveGraph(PlotModel model, string output)
        {
            if (File.Exists(output))
                File.Delete(output);

            using (var stream = File.Create(output))
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter();
                pngExporter.Export(model, stream);
            }
        }
    }
}