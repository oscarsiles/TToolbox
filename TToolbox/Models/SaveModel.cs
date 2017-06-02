using Caliburn.Micro;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TToolbox.DataTypes;

namespace TToolbox.Models
{
    public class SaveModel : IHandle<ExportCsv>, IHandle<ExportXlsx>
    {
        private readonly ILog _log = LogManager.GetLog(typeof(ShellViewModel));
        private readonly IEventAggregator _eventAggregator;
        private string frictionColumnsCsv = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                "Filename",
                "Load (V)",
                "Load (nN)",
                "Trace (V)",
                "Stdev Trace (V)",
                "Retrace (V)",
                "Stdev Retrace (V)",
                "Trace (nN)",
                "Stdev Trace (nN)",
                "Retrace (nN)",
                "Stdev Retrace (nN)",
                "Friction (V)",
                "Stdev Fr (V)",
                "Friction (nN)",
                "Stdev Fr (nN)");

        public SaveModel(IEventAggregator events)
        {
            _eventAggregator = events;
            _eventAggregator.Subscribe(this);
        }

        // Handlers

        public void Handle(ExportCsv export)
        {
            if (export.ExportObj is List<ForceFile>)
            {
                saveForceCsv((List<ForceFile>)export.ExportObj, export.ExportPath, export.IsFilenameOn);
            }
            else if (export.ExportObj is List<FrictionFile>)
            {
                saveFrictionCsv((List<FrictionFile>)export.ExportObj, export.CurrentTip, export.ExportPath);
            }
        }

        public void Handle(ExportXlsx export)
        {
            // debug
            
            if(export.ExportObj is List<ForceFile>)
            {
                // do stuff
            }
            else if (export.ExportObj is List<FrictionFile>)
            {
                // do other stuff
                saveFrictionXlsx((List<FrictionFile>)export.ExportObj, export.CurrentTip, export.ExportPath, export.IsAnalysis);
            }
        }

        // Export Methods

        private void saveForceCsv(List<ForceFile> forceFiles, string output, bool isFilenameOn=false)
        {
            var outputPath = Path.GetDirectoryName(output);

            // columns:
            // 1 - slope (V/nm)
            // 2 - pull-off(V)
            // 3 - pull-off(nm)
            // 4 - max_defl(nm)
            // 5 - filename (?)
            // is filename column wanted?
            string filename = null;
            bool includeFilename = false;

            if (isFilenameOn)
            {
                filename = "Filename";
                includeFilename = true;
            }

            // create our string, then write all to file
            var csv = new StringBuilder();

            // write header row
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},",
                "Slope (V/nm)",
                "Pull-off(V)",
                "Pull-off(nm)",
                "Max Defl(nm)",
                "Pull-off (nN)",
                filename);
            csv.AppendLine(newLine);

            int count = forceFiles.Count;
            foreach (ForceFile file in forceFiles)
            {
                if (includeFilename)
                {
                    filename = file.Filename.Replace(",", "."); // replace , with . or csv columns will break!
                }

                newLine = string.Format("{0},{1},{2},{3},{4},{5},",
                    file.DeflSens,
                    file.PullOffV,
                    file.PullOffNM,
                    file.PullOffNM,
                    file.PullOffNN,
                    filename);
                csv.AppendLine(newLine);
            }

            try
            {
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                File.WriteAllText(output, csv.ToString());

                _eventAggregator.PublishOnUIThread(new StatusBarMessage { LabelText = "Force data saved succesfully!" });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _eventAggregator.PublishOnUIThread(new StatusBarMessage { LabelText = "Error saving force data!" });
            }
        }

        private void saveFrictionCsv(List<FrictionFile> frictionFiles, AFMtip currentTip, string outputPath)
        {
            var csv = new StringBuilder();
            // write header row
            var newLine = frictionColumnsCsv;
            csv.AppendLine(newLine);

            foreach (FrictionFile file in frictionFiles)
            {
                newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                    file.Filename.Replace(",", "."), // fix commas!
                    file.LoadV,
                    file.LoadV * currentTip.DeflSens * currentTip.SpringConst, // will be 0 if either deflSens or springConst not defined
                    file.AvgTraceV,
                    file.StdevTraceV,
                    file.AvgRetraceV,
                    file.StdevRetraceV,
                    file.AvgTraceNN,
                    file.StdevTraceNN,
                    file.AvgRetraceNN,
                    file.StdevRetraceNN,
                    file.AvgFrictionV,
                    file.StdevFrictionV,
                    file.AvgFrictionNN,
                    file.StdevFrictionNN);
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
        }

        private void saveFrictionXlsx(List<FrictionFile> frictionFiles, AFMtip currentTip, string outputPath, bool isAnalysis)
        {
            var excelPackage = new ExcelPackage();
            var sheet = createSheet(excelPackage, "friction test");
            fillFrictionData(sheet, frictionFiles, currentTip);

            // check for analysis
            if (isAnalysis)
            {
                // now lets fill data with our formulas
                var analysis = new AnalysisModel();
                analysis.FrictionFilesAnalysis(frictionFiles, currentTip, ref sheet, frictionColumnsCsv.Split(',').Length + 2);
            }
            
            try
            {
                using (Stream stream = File.Create(outputPath))
                {
                    excelPackage.SaveAs(stream);
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        // excel stuff

        private ExcelWorksheet createSheet(ExcelPackage p, string sheetName)
        {
            p.Workbook.Worksheets.Add(sheetName);
            ExcelWorksheet ws = p.Workbook.Worksheets[p.Workbook.Worksheets.Count];
            ws.Name = sheetName;
            ws.Cells.Style.Font.Size = 11; //Default font size for whole sheet
            ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

            return ws;
        }

        private ExcelWorksheet fillFrictionData(ExcelWorksheet sheet, List<FrictionFile> frictionFiles, AFMtip currentTip)
        {
            var columnNames = frictionColumnsCsv.Split(',');
            foreach (var str in columnNames.Select((value, i) => new { i, value }))
            {
                sheet.Cells[1, str.i + 1].Value = str.value;
            }

            for (int i = 0; i < frictionFiles.Count; i++)
            {
                sheet.Cells[2 + i, 1].Value = frictionFiles[i].Filename;
                sheet.Cells[2 + i, 2].Value = frictionFiles[i].LoadV;
                sheet.Cells[2 + i, 3].Value = frictionFiles[i].LoadV * currentTip.DeflSens * currentTip.SpringConst; // will be 0 if either parameter not defined
                sheet.Cells[2 + i, 4].Value = frictionFiles[i].AvgTraceV;
                sheet.Cells[2 + i, 5].Value = frictionFiles[i].StdevTraceV;
                sheet.Cells[2 + i, 6].Value = frictionFiles[i].AvgRetraceV;
                sheet.Cells[2 + i, 7].Value = frictionFiles[i].StdevRetraceV;
                sheet.Cells[2 + i, 8].Value = frictionFiles[i].AvgTraceNN;
                sheet.Cells[2 + i, 9].Value = frictionFiles[i].StdevTraceNN;
                sheet.Cells[2 + i, 10].Value = frictionFiles[i].AvgRetraceNN;
                sheet.Cells[2 + i, 11].Value = frictionFiles[i].StdevRetraceNN;
                sheet.Cells[2 + i, 12].Value = frictionFiles[i].AvgFrictionV;
                sheet.Cells[2 + i, 13].Value = frictionFiles[i].StdevFrictionV;
                sheet.Cells[2 + i, 14].Value = frictionFiles[i].AvgFrictionNN;
                sheet.Cells[2 + i, 15].Value = frictionFiles[i].StdevFrictionNN;
            }

            return sheet;
        }
    }
}
