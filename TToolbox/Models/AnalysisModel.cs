using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TToolbox.Models
{
    public class AnalysisModel
    {
        public void FrictionFilesAnalysis(List<FrictionFile> frictionFiles, AFMtip currentTip, ref ExcelWorksheet worksheet, int startCol)
        {
            var loadV = new HashSet<double>(); // don't want duplicates, so use hashset
            var loadNN = new List<double>();
            var frictionV = new List<double>();
            var frictionNN = new List<double>();
            var frictionNormV = new List<double>();
            var frictionNormNN = new List<double>();
            var stdErrV = new List<double>();
            var stdErrNN= new List<double>();

            var runCounter = 0;

            // set our column headers
            worksheet.Cells[1, startCol].Value = "Analysis";
            worksheet.Cells[1, startCol + 1].Value = "Load (V)";
            worksheet.Cells[1, startCol + 2].Value = "Load (nN)";
            worksheet.Cells[1, startCol + 3].Value = "Friction (V)";
            worksheet.Cells[1, startCol + 4].Value = "Friction (nN)";
            worksheet.Cells[1, startCol + 5].Value = "Friction Norm (V)";
            worksheet.Cells[1, startCol + 6].Value = "Friction Norm (nN)";
            worksheet.Cells[1, startCol + 7].Value = "Std Err Fr (V)";
            worksheet.Cells[1, startCol + 8].Value = "Std Err Fr (nN)";

            // lets get our load values (in V) into a hashset
            foreach (var file in frictionFiles)
            {
                loadV.Add(file.LoadV);
                // use 1.0V as the baseline for a run - hackish
                if (file.LoadV == 1)
                {
                    runCounter++;
                }
            }
            var loadVOrdered = loadV.OrderBy(x => x).ToList();

            // should we be using excel formulas? lets try..
            for (int i = 0; i < loadVOrdered.Count; i++)
            {
                var row = i + 2; // fix our rows

                worksheet.Cells[row, startCol + 1].Value = loadVOrdered[i];
                worksheet.Cells[row, startCol + 2].Value = loadVOrdered[i] * currentTip.DeflSens * currentTip.SpringConst;
                worksheet.Cells[row, startCol + 3].Formula = String.Format("=AVERAGEIF(B:B,R{0},L:L)", row); // friction V
                worksheet.Cells[row, startCol + 4].Formula = String.Format("=AVERAGEIF(B:B,R{0},N:N)", row); // friction nN
                worksheet.Cells[row, startCol + 5].Formula = String.Format("=T{0}-MIN(T:T)", row); // friction norm V
                worksheet.Cells[row, startCol + 6].Formula = String.Format("=U{0}-MIN(U:U)", row); // friction norm nN
                worksheet.Cells[row, startCol + 7].Formula = String.Format("=AVERAGEIF(B:B,R{0},M:M)/SQRT({1})", row, runCounter); // std error fr V
                worksheet.Cells[row, startCol + 8].Formula = String.Format("=AVERAGEIF(B:B,R{0},O:O)/SQRT({1})", row, runCounter); // std error fr nN
            }
        }
    }
}
