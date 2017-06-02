using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Caliburn.Micro.Logging.NLog;
using MathNet.Numerics;
using Microsoft.Win32;
using TToolbox.DataTypes;
using TToolbox.Models;
using System.IO;
using System.Numerics;

namespace TToolbox.ViewModels
{
    public class ForceTabViewModel : Screen , IHandle<AFMtip>
    {
        private readonly IEventAggregator _eventAggregator;

        private string _forceFilePathText = "Select first input file...";
        private int _lastFileExt = 100;
        private string _resultsLabelText;
        private bool _isSavedCsv = false;
        private bool _isFilenameOn = true;

        private List<ForceFile> _forceFiles = new List<ForceFile>();
        private AFMtip currentTip = new AFMtip();

        public ForceTabViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            this.DisplayName = "Force";
        }

        // Variables

        public string ForceFilePathText
        {
            get { return _forceFilePathText; }
            set { _forceFilePathText = value; NotifyOfPropertyChange(() => ForceFilePathText); }
        }

        public int LastFileExtText
        {
            get { return _lastFileExt; }
            set { _lastFileExt = value; NotifyOfPropertyChange(() => LastFileExtText); }
        }

        public string ResultsLabel
        {
            get { return _resultsLabelText; }
            set { _resultsLabelText = value; NotifyOfPropertyChange(() => ResultsLabel); }
        }

        public bool IsSavedCsv 
        { 
            get 
            { 
                return _isSavedCsv; 
            } 
            set 
            { 
                _isSavedCsv = value; 
                NotifyOfPropertyChange(() => IsSavedCsv); 
            } 
        }

        public bool IsFilenameOn
        {
            get
            {
                return _isFilenameOn;
            }
            set
            {
                _isFilenameOn = value;
                NotifyOfPropertyChange(() => IsFilenameOn);
            }
        }

        public List<ForceFile> ForceFiles
        {
            get { return _forceFiles; }
            set { _forceFiles = value; NotifyOfPropertyChange(() => ForceFiles); }
        }

        // Actions

        public void SelectForceFileButton()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select working NanoScope file",
                //InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "NanoScope File | *.*",
                RestoreDirectory = false
            };

            if ((bool)dlg.ShowDialog())
            {
                // validate force file?
                ForceFilePathText = dlg.FileName;
            }
        }

        public async void CalculateButton()
        {
            // create Force object, validate first, then populate with forceFiles up to last extension
            var force = new ForceModel(currentTip, _eventAggregator);

            if (!force.IsValidFile(ForceFilePathText)) // validate
            {
                // error message goes here
                ResultsLabel = "Please select a valid force file.";
                return;
            }

            ForceFiles = await Task.Run(() => force.CalculateAllPullOff(_forceFilePathText, _lastFileExt));

            var listTemp = force.CalculateAvgStdevPullOff();
            var avgNm = Math.Round(listTemp[0], 4);
            var stdNm = Math.Round(listTemp[1], 4);
            var avgNN = Math.Round(listTemp[2], 4);
            var stdNN = Math.Round(listTemp[3], 4);
            
            string label;
            if (avgNN == 0)
            {
                label = String.Format("Pull-off deflection = {0} \u00B1 {1} nm", avgNm, stdNm);
                
            }
            else
            {
                label = String.Format("Pull-off force = {0} \u00B1 {1} nN", avgNN, stdNN);
                // also publish adhesion force to calibration window!
                _eventAggregator.PublishOnUIThread(new ForceAdhesion { Adhesion = avgNN });
            }

            ResultsLabel = label;

            if (IsSavedCsv)
            {
                saveForceDataCsv();
            }
        }

        public void HistogramButton()
        {
            if (ForceFiles.Count == 0) { return; } // no pull-off has been calculated yet, so do nothing
            if (currentTip.SpringConst == 0 || currentTip.DeflSens == 0)
            {
                //dlgInfoBox("No tip parameters detected.", "Error", TaskDialogIcon.Error);
                return;
            } // no nN force data?

            // create histogram
            WindowManager win = new WindowManager();
            HistogramWinViewModel hist = new HistogramWinViewModel();
            hist.CreateHistogram(ForceFiles);
            win.ShowDialog(hist);
        }

        // Methods

        private void saveForceDataCsv()
        {
            // lets get output path
            // figure out first/last number in series
            int firstNumber = Convert.ToInt32(Path.GetExtension(ForceFilePathText).Replace(".", string.Empty));
            int finalNumber = LastFileExtText;

            string seriesName = Path.GetFileNameWithoutExtension(ForceFiles[0].Filename);
            string outputPath = Path.GetDirectoryName(ForceFilePathText) + @"\" + seriesName + @"\";
            //string output = outputPath + seriesName + ".csv";
            string output = string.Format("{0}{1}-{2}-{3}.csv", outputPath, seriesName, firstNumber.ToString("D3"), finalNumber.ToString("D3"));

            _eventAggregator.PublishOnUIThread(new ExportCsv { ExportObj = _forceFiles, ExportPath = output, IsFilenameOn = this.IsFilenameOn });
        }

        // Handlers

        public void Handle(AFMtip afmtip)
        {
            currentTip = afmtip;
        }
    }
}
