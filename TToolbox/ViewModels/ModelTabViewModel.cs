using Caliburn.Micro;
using MathNet.Numerics;
using Microsoft.Win32;
using OfficeOpenXml;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TToolbox.DataTypes;
using TToolbox.Models;

namespace TToolbox.ViewModels
{
    public class ModelTabViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;

        #region Lifshitz
        private BindableCollection<Solvent> _solventList = new BindableCollection<Solvent>();
        private BindableCollection<Surface> _surfaceList = new BindableCollection<Surface>();
        private BindableCollection<ModelSystem> _systemList = new BindableCollection<ModelSystem>();
        private BindableCollection<int> _noSolidMediaList = new BindableCollection<int>() { 2, 4 };
        private Surface _surface1;
        private Surface _surface2;
        private Surface _surface3;
        private Surface _surface4;
        private string _solventsFilePathText = "Select solvents file...";
        private string _surfacesFilePathText = "Select surfaces file...";
        private string _outputPtFolder = "Select base phasetransfer output folder...";
        private int _noSolidMedia = 2; // set default
        private int _selectedIndex;
        private double _surface3Thickness;
        private double _surface4Thickness;
        private bool _is4solid;

        public ModelTabViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        public string SolventsFilePathText
        {
            get { return _solventsFilePathText; }
            set
            {
                _solventsFilePathText = value;
                NotifyOfPropertyChange(() => SolventsFilePathText);
            }
        }

        public string SurfacesFilePathText
        {
            get { return _surfacesFilePathText; }
            set
            {
                _surfacesFilePathText = value;
                NotifyOfPropertyChange(() => SurfacesFilePathText);
            }
        }

        public string OutputPtFolderPathText
        {
            get { return _outputPtFolder; }
            set
            {
                _outputPtFolder = value;
                NotifyOfPropertyChange(() => OutputPtFolderPathText);
            }
        }

        public BindableCollection<Solvent> SolventList
        {
            get { return _solventList; }
            set
            {
                _solventList = value;
                NotifyOfPropertyChange(() => SolventList);
            }
        }

        public BindableCollection<Surface> SurfaceList
        {
            get
            {
                return _surfaceList;
            }
            set
            {
                _surfaceList = value;
                NotifyOfPropertyChange(() => SurfaceList);
            }
        }

        public BindableCollection<ModelSystem> SystemList
        {
            get { return _systemList; }
            set
            {
                _systemList = value;
                NotifyOfPropertyChange(() => SystemList);
            }
        }

        public BindableCollection<int> NoSolidMediaList
        {
            get { return _noSolidMediaList; }
            set
            {
                _noSolidMediaList = value;
                NotifyOfPropertyChange(() => NoSolidMediaList);
            }
        }

        public Surface Surface1
        {
            get
            {
                return _surface1;
            }
            set
            {
                _surface1 = value;
                NotifyOfPropertyChange(() => Surface1);
            }
        }

        public Surface Surface2
        {
            get
            {
                return _surface2;
            }
            set
            {
                _surface2 = value;
                NotifyOfPropertyChange(() => Surface2);
            }
        }

        public Surface Surface3
        {
            get
            {
                return _surface3;
            }
            set
            {
                _surface3 = value;
                NotifyOfPropertyChange(() => Surface3);
            }
        }

        public Surface Surface4
        {
            get
            {
                return _surface4;
            }
            set
            {
                _surface4 = value;
                NotifyOfPropertyChange(() => Surface4);
            }
        }

        public int NoSolidMedia
        {
            get { return _noSolidMedia; }
            set
            {
                _noSolidMedia = value;
                NotifyOfPropertyChange(() => NoSolidMedia);

                Is4Solid = (NoSolidMedia >= 4); // update our controls
            }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                NotifyOfPropertyChange(() => SelectedIndex);
            }
        }

        public double Surface3Thickness
        {
            get { return _surface3Thickness; }
            set
            {
                _surface3Thickness = value;
                Surface3.Thickness = value;
                NotifyOfPropertyChange(() => Surface3Thickness);
            }
        }

        public double Surface4Thickness
        {
            get { return _surface4Thickness; }
            set
            {
                _surface4Thickness = value;
                Surface4.Thickness = value;
                NotifyOfPropertyChange(() => Surface4Thickness);
            }
        }

        public bool Is4Solid
        {
            get { return NoSolidMedia >= 4; }
            set
            {
                _is4solid = value;
                NotifyOfPropertyChange(() => Is4Solid);
            }
        }

        public void SelectSolventsFileButton()
        {
            SolventsFilePathText = openFileGetPath("Select compatible solvents file", "Solvents file | *.csv");

            // now import the solvents to a list
            try
            {
                if (SolventsFilePathText != null)
                {
                    SolventList = new BindableCollection<Solvent>(importSolvents(SolventsFilePathText));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SelectSurfacesFileButton()
        {
            SurfacesFilePathText = openFileGetPath("Select compatible surfaces file", "Surfaces file | *.csv");

            // now import the surfaces to a list
            try
            {
                if (SurfacesFilePathText != null)
                {
                    SurfaceList = new BindableCollection<Surface>(importSurfaces(SurfacesFilePathText));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SelectOutputPtFolderButton()
        {
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
            dlg.ShowNewFolderButton = true;

            if (dlg.ShowDialog() == true)
            {
                if (!dlg.SelectedPath.EndsWith("output_pt"))
                {
                    //dlgInfoBox("Incorrect phasetransfer output folder detected.", "Error", TaskDialogIcon.Error);
                    return;
                }
                OutputPtFolderPathText = dlg.SelectedPath;
            }
        }

        public async void CalculateLifshitz()
        {
            // let's prefill our boxes (DEBUG)
            if (!OutputPtFolderPathText.EndsWith("output_pt"))
            {
                OutputPtFolderPathText = @"C:\Users\dtp12os\Dropbox\University\PhD\matlab\output_pt";
                SolventsFilePathText = @"C:\Users\dtp12os\Dropbox\University\PhD\matlab\combined\input\solvents.csv";
                SurfacesFilePathText = @"C:\Users\dtp12os\Dropbox\University\PhD\matlab\combined\input\surfaces.csv";

                SolventList = new BindableCollection<Solvent>(importSolvents(SolventsFilePathText));
                SurfaceList = new BindableCollection<Surface>(importSurfaces(SurfacesFilePathText));
                Surface1 = SurfaceList[0];
                Surface2 = Surface1;
            }

            // check if surface/solvent list is empty or not
            if (SolventList.Count == 0 || SurfaceList.Count == 0)
            {
                //dlgInfoBox("Surface and/or solvent list empty.", "Error", TaskDialogIcon.Error);
                return;
            }

            var surfacesTemp = new List<Surface>(NoSolidMedia);
            surfacesTemp.Add(Surface1);
            surfacesTemp.Add(Surface2);
            if (Is4Solid)
            {
                surfacesTemp.Add(Surface3);
                surfacesTemp.Add(Surface4);
            }

            var lifshitz = new ModellingModel(surfacesTemp, SolventList.ToList(), _eventAggregator);

            SystemList.Add(await Task.Run(() => lifshitz.CalculateAll(OutputPtFolderPathText, NoSolidMedia)));
        }

        public void SaveData()
        {
            if (SystemList.Count != 0)
            {
                exportDataXlsx();
            }
        }

        public void ClearData()
        {
            SystemList = new BindableCollection<ModelSystem>();
        }

        public void DeleteSystem(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                try
                {
                    SystemList.RemoveAt(SelectedIndex);
                }
                catch (Exception f)
                {
                    Console.WriteLine(f);
                }
            }
        }

        // CUSTOM METHODS
        private string openFileGetPath(string title, string filter, bool restoreDirectory = false)
        {
            var dlg = new OpenFileDialog
            {
                Title = title,
                //InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = filter,
                RestoreDirectory = restoreDirectory
            };

            if ((bool)dlg.ShowDialog())
            {
                return dlg.FileName;
            }

            return null;
        }

        private string saveFileGetPath(string title, string filter, bool restoreDirectory = false)
        {
            var dlg = new SaveFileDialog
            {
                Title = title,
                //InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = filter,
                RestoreDirectory = restoreDirectory
            };

            if ((bool)dlg.ShowDialog())
            {
                return dlg.FileName;
            }

            return null;
        }

        private List<Solvent> importSolvents(string path)
        {
            var solventList = new List<Solvent>();

            using (var sr = new StreamReader(path))
            {
                var line = sr.ReadLine();
                if (!line.Contains("pdb,"))
                {
                    //dlgInfoBox("No valid solvents file selected.", "Error", TaskDialogIcon.Error);
                    return null; // not a proper file
                }

                solventList.Clear(); // need to clear current afm tips list

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    var parameters = line.Split(',');

                    solventList.Add(new Solvent()
                    {
                        PDB = Convert.ToInt32(parameters[0]),
                        Name = parameters[1],
                        Dielectric = Convert.ToDouble("0" + parameters[2]), // adding 0 to start, to fix any "null" exceptions, safe as dielectric/refractive always +ve
                        Refractive = Convert.ToDouble("0" + parameters[3])
                    });
                }
            }

            return solventList;
        }

        private List<Surface> importSurfaces(string path)
        {
            var surfaceList = new List<Surface>();

            using (var sr = new StreamReader(path))
            {
                var line = sr.ReadLine();
                if (!line.Contains("surface,"))
                {
                    //dlgInfoBox("No valid surfaces file selected.", "Error", TaskDialogIcon.Error);
                    return null; // not a proper file
                }

                //surfaceList.Clear(); // need to clear current afm tips list

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    var parameters = line.Split(',');

                    surfaceList.Add(new Surface()
                    {
                        Name = parameters[0],
                        DielectricBulk = Convert.ToDouble(parameters[1]),
                        RefractiveBulk = Convert.ToDouble(parameters[2]),
                        Ei = Convert.ToDouble(parameters[3]),
                        Ej = Convert.ToDouble(parameters[4]),
                        Type = parameters[5],
                        DielectricChain = Convert.ToDouble(parameters[6]),
                        RefractiveChain = Convert.ToDouble(parameters[7]),
                        VolumeEndGroup = Convert.ToDouble(parameters[8]),
                        DielectricEndGroup = Convert.ToDouble(parameters[9]),
                        RefractiveEndGroup = Convert.ToDouble(parameters[10])
                    });
                }
            }

            // populate surfaces
            Surface1 = surfaceList[0];
            Surface2 = surfaceList[0];
            Surface3 = surfaceList[0];
            Surface4 = surfaceList[0];

            return surfaceList;
        }

        private void exportDataXlsx()
        {
            var excelPackage = new ExcelPackage();

            try
            {
                foreach (var model in SystemList.Select((value, i) => new { i, value }))
                {
                    FillModelSystemSheet(CreateSheet(excelPackage, model.i + ";" + model.value.Name), model.value);
                }

                var savePath = saveFileGetPath("Select save file for modelling data", "Excel Workbook | *.xlsx");

                if (savePath != null) // make sure we don't have a null save path (in case user cancels)
                {
                    using (Stream stream = File.Create(savePath))
                    {
                        excelPackage.SaveAs(stream);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private ExcelWorksheet CreateSheet(ExcelPackage p, string sheetName)
        {
            p.Workbook.Worksheets.Add(sheetName);
            ExcelWorksheet ws = p.Workbook.Worksheets[p.Workbook.Worksheets.Count];
            ws.Name = sheetName;
            ws.Cells.Style.Font.Size = 11; //Default font size for whole sheet
            ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

            return ws;
        }

        private ExcelWorksheet FillModelSystemSheet(ExcelWorksheet sheet, ModelSystem modelSystem)
        {
            sheet.Cells[1, 1].Value = "pdb";
            sheet.Cells[1, 2].Value = "solvent";
            sheet.Cells[1, 3].Value = "e3";
            sheet.Cells[1, 4].Value = "n3";
            sheet.Cells[1, 5].Value = "wad";
            sheet.Cells[1, 6].Value = "dG";

            for (int i = 0; i < modelSystem.PdbList.Count; i++)
            {
                sheet.Cells[i + 2, 1].Value = modelSystem.PdbList[i];
                sheet.Cells[i + 2, 2].Value = modelSystem.LiquidMedia[i].Name;
                sheet.Cells[i + 2, 3].Value = modelSystem.LiquidMedia[i].Dielectric;
                sheet.Cells[i + 2, 4].Value = modelSystem.LiquidMedia[i].Refractive;
                sheet.Cells[i + 2, 5].Value = modelSystem.Wad[i];
                sheet.Cells[i + 2, 6].Value = modelSystem.DG[i];
            }

            return sheet;
        }

        #endregion

        #region LifshitzKramerKronig
        private string _outputTextBox = "";

        public string OutputTextBox
        {
            get { return _outputTextBox; }
            set
            {
                _outputTextBox = value;
                NotifyOfPropertyChange(() => OutputTextBox);
            }
        }

        //dielectric vars
        private double _dielectricWavelength = 0;
        private BindableCollection<Metal> _materialsList = new BindableCollection<Metal>(Metals.MetalList);
        private Metal _selectedMetal = new Metal();

        private double v_1 = 4E13; // at 300K
        private double c = 2.99E8;

        public BindableCollection<Metal> MaterialsList
        {
            get
            {
                return _materialsList;
            }
        }

        public Metal SelectedMetal
        {
            get { return _selectedMetal; }
            set 
            {
                _selectedMetal = value;
                NotifyOfPropertyChange(() => SelectedMetal);
            }
        }

        public double DielectricWavelength
        {
            get { return _dielectricWavelength; }
            set
            {
                _dielectricWavelength = value;
                NotifyOfPropertyChange(() => DielectricWavelength);
            }
        }

        public void CalculateDielectric()
        {
            var dielectric = Dielectric.Dielectric_Calc(_dielectricWavelength * 1E-9, _selectedMetal.Name);            

            writeToOutput(String.Format("Dielectric for {0} at {1}nm: {2} + {3}i", _selectedMetal.Name, _dielectricWavelength, dielectric.Item1.ToString("F"), dielectric.Item2.ToString("F")));
        }

        public void TestButton()
        {
            //Control.MaxDegreeOfParallelism = Environment.ProcessorCount - 1; // needed?

            var d_0 = 0.165E-9;

            var sur1 = "au";
            var sur1d = "au";
            var sur2 = "sam";
            var sur2d = "sam";
            var sol = "decane";

            //var bla = calcHamakerKramerKronig("sam", "sam", "air");
            //var bla1 = calcHamakerKramerKronig("sam", "sam", "h2o");
            //var bla2 = calcHamakerKramerKronig("au", "au", "h2o";)
            //var bla3 = calcHamakerKramerKronig("sam", "sam", "ethanol");
            //var bla4 = calcHamakerKramerKronig("sam", "sam", "methanol");
            //var bla5 = calcHamakerKramerKronig("sam", "sam", "heptane");

            //var test1 = Dielectric.imFreqDielectricCalc_nonmetal(1E18, "sam");
            //var test2 = Dielectric.kramersKronigImFreq(1E18, "au");

            //var B131d_ti = calcHamakerKramerKronig("ti", "ti", sol);
            //var B131d_au = calcHamakerKramerKronig("au", "au", sol);
            //var B131d_ti_etoh = calcHamakerKramerKronig("ti", "ti", "decane");
            //var B131d_au_etoh = calcHamakerKramerKronig("au", "au", "decane");

            var A232d = calcHamakerKramerKronig(sur2, sur2d, sol);
            var A121 = calcHamakerKramerKronig(sur1, sur1, sur2);
            var A32d3 = calcHamakerKramerKronig(sol, sol, sur2d);
            var A1d2d1d = calcHamakerKramerKronig(sur1d, sur1d, sur2d);
            var A323 = calcHamakerKramerKronig(sol, sol, sur2);
            var A131d = calcHamakerKramerKronig(sur1, sur1d, sol);
            

            var T = 1e-9;
            var Td = 1e-9;

            var list = new List<Tuple<double, double>>();
            var N = 50;
            var Tfinal = 2E-9;
            for (int i = 0; i < N; i++)
            {
                T = 0.5E-10 + (Tfinal/N) * i;
                Td = T;

                Func<double, double> wad5ForceFunc = x => (1 / (6 * Math.PI)) * ((A232d / (Math.Pow(x, 3))) - (Math.Sqrt(A121 * A32d3) / (Math.Pow(x + T, 3))) - (Math.Sqrt(A1d2d1d * A323) / (Math.Pow(x + Td, 3))) + (Math.Sqrt(A1d2d1d * A121) / (Math.Pow(x + T + Td, 3)))); // x = D


                
                var wad5 = Integrate.OnClosedInterval(wad5ForceFunc, d_0, 1); // setting upper limit to 1(meter) stops weird results, as function not normalized to y=0 it seems

                list.Add(new Tuple<double, double>(T, wad5));

            }

            //Func<double, double> wad5ForceFunc = x => ((1 / (6 * Math.PI)) * (A232d / (Math.Pow(x, 3)) - (Math.Sqrt(A121 * A32d3)) / (Math.Pow(x + T1, 3)) - (Math.Sqrt(A1d2d1d * A323)) / (Math.Pow(x + T2, 3)) + (Math.Sqrt(A1d2d1d * A121)) / (Math.Pow(x + T1 + T2, 3)))); // x = D
            //var wad5 = Integrate.OnClosedInterval(wad5ForceFunc, d_0, 1); // setting upper limit to 1(meter) stops weird results, as function not normalized to y=0 it seems

            using (var sw = new StreamWriter(@"C:\test\test_" + sol + ".csv"))
            {
                foreach (var item in list)
                {
                    sw.WriteLine(String.Format("{0},{1}", item.Item1, item.Item2));
                }
            }
        }

        public void ClearOutputButton()
        {
            OutputTextBox = "";
        }

        // CUSTOM METHODS
        private double calcHamakerKramerKronig(string mat1, string mat2, string mat3)
        {
            var c = 2.99E8;
            var k = 1.38E-23;
            var h = 6.63E-34;
            var T = 298.0; // was this the cause of ALL the issues?! (int instead of double)
            var d_0 = 0.165E-9;
            var minFreq = 4E13; // 1E13-1E14 works fine
            var maxFreq = 1E20;

            Console.WriteLine("{0}/{2}/{1}", mat1, mat2, mat3);

            var e1 = Dielectric.Dielectric_Calc(double.PositiveInfinity, mat1).Item1;
            var e2 = Dielectric.Dielectric_Calc(double.PositiveInfinity, mat2).Item1;
            var e3 = Dielectric.Dielectric_Calc(double.PositiveInfinity, mat3).Item1;

            var integral = Dielectric.LifshitzImaginaryFreqIntegral(mat1, mat2, mat3, minFreq, maxFreq);
            var A = (3.0 / 4) * k * T * ((e1 - e3) / (e1 + e3)) * ((e2 - e3) / (e2 + e3))
                    + ((3 * h) / (4 * Math.PI)) * integral;

            var A2 = (3.0 / 4) * k * T * ((e1 - e3) / (e1 + e3)) * ((e2 - e3) / (e2 + e3));
            var A3 = ((3 * h) / (4 * Math.PI)) * integral;


            var Wad = A / (12 * Math.PI * Math.Pow(d_0, 2)); // debug


            return A;
        }

        private void writeToOutput(string line)
        {
            var sb = new StringBuilder(OutputTextBox);

            //sb.AppendLine("testing" + sb.ToString().Split('\n').Length);
            sb.Prepend(line + Environment.NewLine);
            OutputTextBox = sb.ToString();
        }

        #endregion
        // EVENT HANDLERS
    }
}