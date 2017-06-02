using Caliburn.Micro;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TToolbox.DataTypes;
using TToolbox.ViewModels;

namespace TToolbox.Models
{
    public class ForceModel
    {
        private readonly IEventAggregator _eventAggregator;

        private List<ForceFile> _forceFiles = new List<ForceFile>();
        private AFMtip currentTip = new AFMtip();

        public List<ForceFile> ForceFiles
        {
            get { return _forceFiles; }
            set { _forceFiles = value; }
        }

        public ForceModel(AFMtip tip, IEventAggregator events)
        {
            _eventAggregator = events;
            currentTip = tip;
        }

        public bool IsValidFile(string inputPath)
        {
            var ns = new NanoscopeModel();
            return ns.GetFileType(inputPath) == 0; // 0 for force, 1 for friction, 2 for not valid nanoscope file
        }

        public List<ForceFile> CalculateAllPullOff(string firstFilePath, int lastExt)
        {
            string filename = firstFilePath;

            // diagnostics
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            string baseName = Path.GetFileNameWithoutExtension(filename);
            string firstExt = Path.GetExtension(filename).Replace(".", string.Empty); // need to remove period
            string finalExt = lastExt.ToString("D3"); // ensures extension is properly padded
            string workingDir = Path.GetDirectoryName(filename);
            //string backupDir = System.IO.Path.Combine(workingDir, "backup");
            int initialNumber = Convert.ToInt32(firstExt);
            int finalNumber = Convert.ToInt32(finalExt);
            string path1 = Path.Combine(workingDir, baseName + "." + firstExt);

            // clear previous results (if any?)
            //_forceFiles.Clear();

            // check if valid force file
            try
            {
                NanoscopeModel ns = new NanoscopeModel(Path.Combine(workingDir, baseName + "." + firstExt));
                ns.Calc_pullOff();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //dlgInfoBox("Could not read force file.\nOnly newer NanoScope files are valid.", "Error", TaskDialogIcon.Error);
                return null;
            }

            int j = 0;
            Parallel.For(initialNumber, finalNumber, i =>
            {
                string extension = i.ToString("D3");
                string path2 = Path.Combine(workingDir, baseName + "." + extension);

                // check to see if file exists, if not skip
                if (File.Exists(path2))
                {
                    // init ns object
                    NanoscopeModel ns = new NanoscopeModel(path2);

                    //ns.Calc_pullOff();

                    try
                    {
                        // calculate pull-off data
                        ns.Calc_pullOff();

                        // save data to list
                        _forceFiles.Add(new ForceFile()
                        {
                            FullPath = path2,
                            Directory = workingDir,
                            Filename = baseName + "." + extension,
                            Index = i,
                            B = ns.B,
                            Va = ns.Va,
                            Vr = ns.Vr,
                            DeflSens = ns.DeflSens,
                            PullOffV = ns.PullOffV,
                            PullOffNM = ns.PullOffNM,
                            PullOffNN = ns.PullOffNM * currentTip.SpringConst, // convert from nm to nN (spring constant N/m = nN/nm)
                            MaxDefl = ns.MaxDefl
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                }
                else
                {
                    //DEBUG
                    //Console.WriteLine("File not found.");
                }
                //setProgress(j, finalNumber);
                setProgress((int)(100 * j / finalNumber));
                Interlocked.Increment(ref j);
            });

            setProgress(0);
            return _forceFiles;
        }

        public List<double> CalculateAvgStdevPullOff()
        {
            var list = new List<double>();
            var pullOffNMTemp = new List<ForceFile>(_forceFiles).OfType<ForceFile>().Select(forceFile => forceFile.PullOffNM).ToList(); // OfType fixes nullRefException!
            var pullOffNNTemp = new List<ForceFile>(_forceFiles).OfType<ForceFile>().Select(forceFile => forceFile.PullOffNN).ToList(); // OfType fixes nullRefException!

            double avg_pullOffNM = pullOffNMTemp.Mean();
            double std_pullOffNM = pullOffNMTemp.StandardDeviation();
            double avg_pullOffNN = pullOffNNTemp.Mean();
            double std_pullOffNN = pullOffNNTemp.StandardDeviation();

            list.Add(avg_pullOffNM);
            list.Add(std_pullOffNM);
            list.Add(avg_pullOffNN);
            list.Add(std_pullOffNN);

            return list;
        }

        private void setProgress(int percent)
        {
            Execute.OnUIThread(() =>
            {
                _eventAggregator.PublishOnUIThread(new StatusBarMessage { ProgressPercent = percent });
            });
        }
    }
}