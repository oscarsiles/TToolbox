using Caliburn.Micro;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TToolbox.DataTypes;
using TToolbox.Models;

namespace TToolbox.ViewModels
{
    public class FrictionTabViewModel : Screen , IHandle<AFMtip> , IHandle<ForceAdhesion>
    {
        private readonly IEventAggregator _eventAggregator;

        // VARIABLES
        private AFMtip currentTip = new AFMtip();

        #region experimental
        private BindableCollection<FrictionFile> _frictionFiles = new BindableCollection<FrictionFile>();
        private List<string> _saveFileTypes = new List<string> { "csv", "xlsx" };
        private string _saveFileTypeSelected = "xlsx";
        private bool _isSavedFile = false;
        private bool _isAnalysis = false;

        public List<string> SaveFileTypes
        {
            get
            {
                return _saveFileTypes;
            }
        }

        public string SaveFileTypeSelected
        {
            get { return _saveFileTypeSelected; }
            set
            {
                _saveFileTypeSelected = value;
                NotifyOfPropertyChange(() => SaveFileTypeSelected);
                NotifyOfPropertyChange(() => IsAnalysisEnabled);
            }
        }

        public BindableCollection<FrictionFile> FrictionFiles
        {
            get { return _frictionFiles; }
            set
            {
                _frictionFiles = value;
                NotifyOfPropertyChange(() => FrictionFiles);
            }
        }

        public bool IsSavedFile
        {
            get
            {
                return _isSavedFile;
            }
            set
            {
                _isSavedFile = value;
                NotifyOfPropertyChange(() => IsSavedFile);
                NotifyOfPropertyChange(() => IsAnalysisEnabled);
            }
        }

        public bool IsAnalysis
        {
            get { return _isAnalysis; }
            set
            {
                _isAnalysis = value;
                NotifyOfPropertyChange(() => IsAnalysis);
            }
        }

        public bool IsAnalysisEnabled
        {
            get { return (IsSavedFile && (SaveFileTypeSelected == "xlsx")); }
        }
        #endregion

        // calibration
        #region calibration
        private BindableCollection<FrictionFile> _calibrationFiles = new BindableCollection<FrictionFile>();
        private double _calibrationAdhesion;

        public BindableCollection<FrictionFile> CalibrationFiles
        {
            get { return _calibrationFiles; }
            set
            {
                _calibrationFiles = value;
                NotifyOfPropertyChange(() => CalibrationFiles);
            }
        }

        public double CalibrationAdhesion
        {
            get { return _calibrationAdhesion; }
            set
            {
                _calibrationAdhesion = value;
                NotifyOfPropertyChange(() => CalibrationAdhesion);
            }
        }
        #endregion

        public FrictionTabViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        // Actions

        #region experimental
        public void AddFrictionFiles()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "Friction file(s)";
            dlg.Multiselect = true;

            // lets add friction files
            if (!(bool)dlg.ShowDialog())
            {
                return; // exit if cancelled
            }

            addFrictionFiles(dlg.FileNames, FrictionFiles);
        }

        public void RemoveFrictionFiles()
        {
            FrictionFiles.Clear();
            GC.Collect(); // clean up memory
        }

        public void ViewFrictionGraph()
        {

        }

        public async void CalculateFriction()
        {
            if (FrictionFiles.Count == 0) // do nothing if no files selected
            {
                return;
            }

            var fr = new FrictionModel(FrictionFiles.ToList(), currentTip, _eventAggregator);
            FrictionFiles = new BindableCollection<FrictionFile>(await Task.Run(() => fr.CalculateAllFriction()));

            // save csv if checkbox marked
            if (IsSavedFile)
            {
                saveFrictionData();
            }
        }
        #endregion

        #region calibration
        public void AddCalibrationFiles()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "Friction file(s)";
            dlg.Multiselect = true;

            // lets add friction files
            if (!(bool)dlg.ShowDialog())
            {
                return; // exit if cancelled
            }

            addFrictionFiles(dlg.FileNames, CalibrationFiles);
        }

        public void RemoveCalibrationFiles()
        {
            CalibrationFiles.Clear();
            GC.Collect(); // clean up memory
        }

        public async void CalculateCalibration()
        {
            if (CalibrationFiles.Count == 0) // do nothing if no files selected
            {
                return;
            }

            // first calculate friction
            var fr = new FrictionModel(CalibrationFiles.ToList(), currentTip, _eventAggregator);
            CalibrationFiles = new BindableCollection<FrictionFile>(await Task.Run(() => fr.CalculateAllFriction()));
            CalibrationFiles = new BindableCollection<FrictionFile>(await Task.Run(() => fr.CalculateCalibration(_calibrationAdhesion)));
        }
        #endregion

        // File Drop logic

        public void FilePreviewDragEnter(DragEventArgs e)
        {
            e.Handled = true;
        }

        public void FileDropped(DragEventArgs e, string elementSection)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                if (elementSection.ToLower() == "experimental")
                {
                    addFrictionFiles(files, FrictionFiles);
                }
                else if (elementSection.ToLower() == "calibration")
                {
                    addFrictionFiles(files, CalibrationFiles);
                }
            }
        }

        // Handlers

        public void Handle(AFMtip afmTip)
        {
            currentTip = afmTip;
        }

        public void Handle(ForceAdhesion adhesion)
        {
            CalibrationAdhesion = adhesion.Adhesion;
        }

        // Methods
        private void addFrictionFiles(string[] files, BindableCollection<FrictionFile> friction)
        {
            foreach (String file in files)
            {
                // check if file is valid, if not skip
                var ns = new NanoscopeModel();
                if (ns.GetFileType(file) != 1)
                {
                    continue;
                }

                // want to find load (parses the following: 0.4V, 0,4v, 1V, 0.4V_2)
                double load = 0;

                try
                {
                    string regexLoad = @"[+-]?(\d+([\.]\d+)?[V])"; // useful: www.regexr.com
                    //string regexLoad = @"[+-]?(\d+([\.\,]\d+)?[V])|(([\.\,])+[V])"; // original match string (reduced version above, requires removal of spaces and commas)

                    load = Regex.Matches(Path.GetFileNameWithoutExtension(file).ToUpper().Replace(",", ".").Replace(" ", ""), regexLoad) // this returns all the matches in input
                                       .Cast<Match>() // this casts from MatchCollection to IEnumerable<Match>
                                       .Select(x => double.Parse(x.Value.Replace("V", ""))) // this parses each of the matched string to double
                                       .ToArray() // this converts IEnumerable<decimal> to an Array of decimal
                                       .Last(); // this extracts the last number
                }
                catch
                {
                    // do nothing
                    return;
                }

                friction.Add(new FrictionFile()
                {
                    FullPath = file,
                    Directory = Path.GetDirectoryName(file),
                    Filename = Path.GetFileName(file),
                    LoadV = Convert.ToDouble(load)
                });
            }
        }

        private void saveFrictionData()
        {
            try
            {
                string filter = "";
                if (SaveFileTypeSelected == "csv")
                {
                    filter = "Comma separated values (*.csv)|*.csv";
                }
                else if (SaveFileTypeSelected == "xlsx")
                {
                    filter = "Excel Workbook (*.xlsx)|*.xlsx";
                }

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.AddExtension = true;
                dlg.DefaultExt = "." + SaveFileTypeSelected;
                dlg.Filter = filter;
                dlg.InitialDirectory = FrictionFiles[0].Directory;

                if ((bool)dlg.ShowDialog())
                {
                    object obj = null;
                    if (SaveFileTypeSelected == "csv")
                    {
                        obj = new ExportCsv { ExportObj = FrictionFiles.ToList(), ExportPath = dlg.FileName, CurrentTip = currentTip };
                    }   
                    else if (SaveFileTypeSelected == "xlsx")
                    {
                        obj = new ExportXlsx { ExportObj = FrictionFiles.ToList(), ExportPath = dlg.FileName, CurrentTip = currentTip , IsAnalysis = this.IsAnalysis };
                    }

                    _eventAggregator.PublishOnUIThread(obj);
                }
            }
            catch (Exception e)
            {
                // do nothing?
            }
        }
    }
}
