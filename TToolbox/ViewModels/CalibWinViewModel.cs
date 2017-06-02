using Caliburn.Micro;
using OxyPlot;
using OxyPlot.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TToolbox.ViewModels
{
    public class CalibWinViewModel : Screen
    {
        private PlotModel _plotModel = new PlotModel();
        private BindableCollection<Item> _trace;
        private BindableCollection<Item> _retrace;

        public PlotModel PlotModel
        {
            get { return _plotModel; }
            set
            {
                _plotModel = value;
                NotifyOfPropertyChange(() => PlotModel);
            }
        }
        public BindableCollection<Item> Trace
        {
            get { return _trace; }
            set
            {
                _trace = value;
                NotifyOfPropertyChange(() => Trace);
            }
        }
        public BindableCollection<Item> Retrace
        {
            get { return _retrace; }
            set
            {
                _retrace = value;
                NotifyOfPropertyChange(() => Trace);
            }
        }

        public CalibWinViewModel()
        {
            this.DisplayName = "Lateral spring constant calibration";
        }

        public void NextFile()
        {
            TryClose();
        }

        public void Add_Annotation(int x, OxyColor color)
        {
            var la = new LineAnnotation { Type = LineAnnotationType.Vertical, X = x, Color = color };
            la.MouseDown += (s, e) =>
            {
                if (e.ChangedButton != OxyMouseButton.Left)
                {
                    return;
                }

                la.StrokeThickness *= 2;
                //WindowPlotModel.RefreshPlot(false);
                PlotModel.InvalidatePlot(true);
                e.Handled = true;
            };

            // Handle mouse movements (note: this is only called when the mousedown event was handled)
            la.MouseMove += (s, e) =>
            {
                la.X = la.InverseTransform(e.Position).X;
                //WindowPlotModel.RefreshPlot(false);
                PlotModel.InvalidatePlot(true);
                e.Handled = true;
            };
            la.MouseUp += (s, e) =>
            {
                la.StrokeThickness /= 2;
                //WindowPlotModel.RefreshPlot(false);
                PlotModel.InvalidatePlot(true);
                e.Handled = true;
            };
            PlotModel.Annotations.Add(la);
            //return model;
        }

        public List<int> Get_AnnotationsX()
        {
            var xValues = new List<int>();

            var list = PlotModel.Annotations.ToList();

            foreach (LineAnnotation la in list)
            {
                xValues.Add((int)la.X);
            }

            xValues.Sort();

            return xValues;
        }
    }
}
