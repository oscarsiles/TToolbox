using Caliburn.Micro;
using MathNet.Numerics.Statistics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TToolbox.ViewModels
{
    public class HistogramWinViewModel : Screen
    {
        private PlotModel _plotModel;
        private BindableCollection<Item> _items;

        public PlotModel PlotModel
        {
            get { return _plotModel; }
            set
            {
                _plotModel = value;
                NotifyOfPropertyChange(() => PlotModel);
            }
        }
        public BindableCollection<Item> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                NotifyOfPropertyChange(() => Items);
            }
        }

        public HistogramWinViewModel()
        {
            this.DisplayName = "Force Histogram";
        }

        public void CreateHistogram(List<ForceFile> forceFiles)
        {
            // histogram, so let's create our bin list and our data list
            var listData = new List<double>();
            var listBins = new List<double>();
            var listFreq = new List<int>();
            var noBins = 20; // how many bins - user changeable in future?

            foreach (ForceFile file in forceFiles)
            {
                try
                {
                    listData.Add((double)file.PullOffNN);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            var minRound = Math.Round(listData.Min(), 2);
            var maxRound = Math.Round(listData.Max(), 2);
            var range = maxRound - minRound;
            var intervalSize = Math.Round(range / noBins, 2);

            // now lets find frequencies
            var histogram = new Histogram(listData, noBins);

            // want Count (freq) and LowerBound (value of bin start)
            for (int i = 0; i < noBins; i++) // time to add bins
            {
                listBins.Add(Math.Round(minRound + i * intervalSize, 2)); // get our own nice bin labels (close enough to numerics one)
                listFreq.Add(Convert.ToInt32(histogram[i].Count));
            }

            PlotModel = createHistogramPlot(listBins, listFreq);
        }

        private PlotModel createHistogramPlot(List<double> listBins, List<int> listFreq)
        {
            // import data
            Items = new BindableCollection<Item>();

            for (int i = 0; i < listBins.Count; i++)
            {
                this.Items.Add(new Item { Label = listBins[i].ToString(), Value1 = listFreq[i] });
            }


            var plotModel = new PlotModel
            {
                Title = "Pull-off force histogram",
                IsLegendVisible = false,
                
            };

            // setup axes
            plotModel.Axes.Add(new CategoryAxis {
                Title = "\nPull-off force (nN)", // add blank line to fix spacing
                ItemsSource = this.Items,
                LabelField = "Label",
                Angle = -45,
                IsPanEnabled = false,
                IsZoomEnabled = false
            });

            plotModel.Axes.Add(new LinearAxis {
                Title = "Frequency\n",
                Position = AxisPosition.Left,
                MinimumPadding = 0,
                //MaximumPadding = (int)Math.Round((double)listFreq.Max() / 10), // for some reason this sets the wrong maximum...
                AbsoluteMinimum = 0,
                AbsoluteMaximum = listFreq.Max() + (int)Math.Round((double)listFreq.Max() / 10),
                IsPanEnabled = false,
                IsZoomEnabled = false
            });

            // setup data
            plotModel.Series.Add(new ColumnSeries { Title = "Frequency", ItemsSource = this.Items, ValueField = "Value1"});

            return plotModel;
        }
    }

    public class Item
    {
        public string Label { get; set; }
        public int Value1 { get; set; }
    }
}
