using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.Win32;
using System.Data;
using System.IO;
using TToolbox.DataTypes;
using System.Windows;

namespace TToolbox.ViewModels
{
    public class AfmTipTabViewModel : Screen , IHandle<FrictionAlpha>
    {
        private readonly IEventAggregator _eventAggregator;
        private AFMtip _selectedTip;
        private BindableCollection<AFMtip> _afmTips;

        public AfmTipTabViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            AfmTips = new BindableCollection<AFMtip>();
        }

        public BindableCollection<AFMtip> AfmTips 
        { 
            get 
            { 
                return _afmTips; 
            } 
            set 
            { 
                _afmTips = value; 
                NotifyOfPropertyChange(() => AfmTips); 
            } 
        }

        public AFMtip SelectedTip
        { 
            get 
            {
                if (_selectedTip !=null)
                    return _selectedTip;
                return new AFMtip();
            } 
            set 
            { 
                _selectedTip = value;
                UpdateAllFields();
            } 
        }

        public string TipName
        {
            get { return SelectedTip.Name; }
            set 
            { 
                SelectedTip.Name = value;
                UpdateAllFields();
            }
        }

        public double DeflSens
        {
            get { return SelectedTip.DeflSens; }
            set { SelectedTip.DeflSens = value; UpdateAllFields(); }
        }

        public double Radius
        {
            get { return SelectedTip.Radius; }
            set { SelectedTip.Radius = value; UpdateAllFields(); }
        }

        public double SpringConst
        {
            get { return SelectedTip.SpringConst; }
            set { SelectedTip.SpringConst = value; UpdateAllFields(); }
        }

        public double LateralSpringConst
        {
            get { return SelectedTip.FrictionAlpha; }
            set { SelectedTip.FrictionAlpha = value; UpdateAllFields(); }
        }

        // Actions

        public void CreateTip()
        {
            AfmTips.Add(new AFMtip { Name = "New tip" + AfmTips.Count });
            SelectedTip = AfmTips.LastOrDefault();
        }

        public void RemoveTip()
        {
            AfmTips.Remove(SelectedTip);
            SelectedTip = AfmTips.LastOrDefault();
        }

        public void SaveTips()
        {
            var dlg = new SaveFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = ".csv";
            dlg.Filter = "Comma separated values (*.csv)|*.csv";

            if ((bool)dlg.ShowDialog())
            {
                string outputPath = dlg.FileName;

                var csv = new StringBuilder();

                var newLine = string.Format("{0},{1},{2},{3},{4},",
                    "Tip Name",
                    "Defl Sens (nm/V)",
                    "Radius (nm)",
                    "Spring Constant (N/m)",
                    "Lateral Stiffness (nN/V)");
                csv.AppendLine(newLine);

                foreach (AFMtip tip in AfmTips)
                {
                    newLine = string.Format("{0},{1},{2},{3},{4},",
                        tip.Name.Replace(".", ","), // fix commas, just in case
                        tip.DeflSens,
                        tip.Radius,
                        tip.SpringConst,
                        tip.FrictionAlpha);
                    csv.AppendLine(newLine);
                }
                if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                }

                try
                {
                    File.WriteAllText(outputPath, csv.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    //dlgInfoBox("Cannot write to file.\nPlease check if file is open in another program and try again.", "Error", TaskDialogIcon.Error);
                }
                _eventAggregator.PublishOnUIThread(new StatusBarMessage { LabelText = "Afm tips successfully saved!" });
            }
           
        }

        public void LoadTips()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Comma separated values (*.csv)|*.csv";

            if ((bool)dlg.ShowDialog())
            {
                loadTipsFromCsv(dlg.FileName);
            }
        }

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
                if (elementSection.ToLower() == "tips" && files.Count() == 1)
                {
                    loadTipsFromCsv(files[0]);
                }
            }
        }

        // Methods

        private void UpdateAllFields()
        {
            NotifyOfPropertyChange(() => AfmTips);
            NotifyOfPropertyChange(() => SelectedTip);
            NotifyOfPropertyChange(() => TipName);
            NotifyOfPropertyChange(() => DeflSens);
            NotifyOfPropertyChange(() => Radius);
            NotifyOfPropertyChange(() => SpringConst);
            NotifyOfPropertyChange(() => LateralSpringConst);

            // lets also publish the current selected AFM tip
            _eventAggregator.PublishOnUIThread(SelectedTip);
        }

        private void loadTipsFromCsv(string filepath)
        {
            // marginally faster to use CsvReader
            AfmTips.Clear();

            var csv = new CSV();
            var ds = csv.ReadCSV(filepath, true);

            //if (!Convert.ToString(ds.Tables[0].Columns[0].ColumnName).Contains("Tip Name")) { dlgInfoBox("Invalid tips file selected.", "Error", TaskDialogIcon.Error); return; }

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                AfmTips.Add(new AFMtip()
                {
                    Name = Convert.ToString(row[0]),
                    DeflSens = Convert.ToDouble(row[1]),
                    Radius = Convert.ToDouble(row[2]),
                    SpringConst = Convert.ToDouble(row[3]),
                    FrictionAlpha = Convert.ToDouble(row[4])
                });
            }
            //NotifyOfPropertyChange(() => AfmTips);
            SelectedTip = AfmTips.FirstOrDefault();

            _eventAggregator.PublishOnUIThread(new StatusBarMessage { LabelText = "Successfully loaded AFM tips!" });
        }

        // Handlers
        public void Handle(FrictionAlpha alpha)
        {
            LateralSpringConst = alpha.Alpha;
        }
    }
}
