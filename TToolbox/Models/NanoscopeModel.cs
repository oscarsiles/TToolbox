using CustomFileIO;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TToolbox.Models
{
    internal class NanoscopeModel
    {
        // Port Carpick's matlab code (modified to work with newer NanoScope files) into C#.
        // Other than a few modifications and optimizations, code is from Carpick.
        //
        // For example, carpick opens each file several times to read header/data.
        // Instead, we will read whole file to memory, and read from that (much lower IO bottleneck).

        private string inputPath;
        private MemoryStream inputMStream;
        private List<int> pos_spl = new List<int>();
        private List<int> pos_data = new List<int>();
        private List<int> pos_ramp = new List<int>();
        private List<int> pos_sensdef = new List<int>();
        private List<int> pos_senszscan = new List<int>();
        private List<int> scal_data = new List<int>();
        private List<int> pos_lineno = new List<int>();
        private List<int> file_type_data = new List<int>();

        // x/y axes lists
        private List<double> a = new List<double>(); // raw

        public List<double> B = new List<double>();
        public List<double> Va = new List<double>(); // extend
        public List<double> Vr = new List<double>(); // retract

        // image data list (3D nested lists?)
        private double[][][] img_data = new double[4][][]; // 4 channels ; double[channel][x][y] -- Do we actually need all the img data? or just (re)trace below? Keep for now...

        // list is each line, double[] is every X on that line
        public List<double[]> TraceData = new List<double[]>();

        public List<double[]> RetraceData = new List<double[]>();

        // lets create trace-minus-retrace data too
        public List<double> TMR = new List<double>();

        public double[] TraceLineAvg, RetraceLineAvg, LineAvg, LineAvgFricV;

        // public variables
        public double? DeflSens;

        public double? PullOffV;
        public double? PullOffNM;
        public double? MaxDefl;
        public double AvgFricNewV, StdevFricNewV;
        public double AvgFricTrV, StdFricTrV;
        public double AvgFricRetrV, StdFricRetrV;

        private double spl, senszscan, ramp, imag_pos, scaling, hscale, lineno;

        public NanoscopeModel() { }

        public NanoscopeModel(string input)
        {
            inputPath = input;

            // read file to memoryStream
            inputMStream = loadFileToMemoryStream(inputPath);

            // load parameters into variables
            pos_spl = di_header_find(inputMStream, @"\Samps/line:");
            pos_data = di_header_find(inputMStream, @"\Data offset");
            scal_data = di_header_find(inputMStream, @"\@4:Z scale: V [Sens.");
            pos_senszscan = di_header_find(inputMStream, @"\@Sens. Zsens:");
            pos_ramp = di_header_find(inputMStream, @"Ramp size Zsweep");
            pos_sensdef = di_header_find(inputMStream, @"Sens. DeflSens");
        }

        public void Calc_pullOff()
        {
            // extract numbers from file positions to variables
            spl = extract_num(inputMStream, pos_spl[1]);
            senszscan = extract_num(inputMStream, pos_senszscan[0]);
            ramp = extract_num(inputMStream, pos_ramp[0], 1);
            imag_pos = extract_num(inputMStream, pos_data[0]);
            scaling = extract_num(inputMStream, scal_data[0], 1);

            hscale = senszscan * ramp * Math.Pow(2, 16) / spl;

            // go to first pixel, and start reading
            using (BinaryReader br = new BinaryReader(inputMStream))
            {
                inputMStream.Position = (int)imag_pos;

                int totalX = 2 * (int)spl;

                // read data into array, then
                for (int i = 0; i < totalX; i++)
                {
                    double x = (double)br.ReadInt16() * scaling;
                    a.Add(x);

                    // split extend/retract data
                    if (i < totalX / 2)	// extend
                    {
                        Va.Add(a[i]);
                    }
                    else // retract
                    {
                        Vr.Add(a[i]);
                    }

                    // convert x axis to nm
                    if (i >= 1 && i <= 512)
                    {
                        x = hscale * i / 10;
                        B.Add(x);
                    }
                }
            }

            // From carpick:
            //  Detemine whether or not a portion of the in-contact curve is flat (ie:
            //  did the tip deflect so much that the laser moved beyond the detector?)
            //  and find the index of the end of the flat part (end_plateau)

            //  First smooth the data to clean up some noise (NOTE: this is only to
            //  determine if there's a plateau. The smoothened data is not analyzed in
            //  any other way).
            var smooth_va = Va;
            int smooth_range = 5;

            int imin;
            int imax;
            for (int i = 1; i < (int)spl; i++)
            {
                if ((i + 1) - smooth_range < 1)
                {
                    imin = 1;
                    imax = (i + 1) + smooth_range;
                }
                else if ((i + 1) + smooth_range > spl)
                {
                    imin = (i + 1) - smooth_range;
                    imax = (int)spl;
                }
                else
                {
                    imin = (i + 1) - smooth_range;
                    imax = (i + 1) + smooth_range;
                }

                List<double> bPoly = new List<double>();
                List<double> vaPoly = new List<double>();

                for (int j = imin - 1; j < i; j++)
                {
                    bPoly.Add(B[j]);
                    vaPoly.Add(Va[j]);
                }

                for (int j = i + 1; j < imax; j++)
                {
                    bPoly.Add(B[j]);
                    vaPoly.Add(Va[j]);
                }

                //var p = polyFitPlus(bPoly, vaPoly, 2);
                var p = polyFit(bPoly, vaPoly, 2);

                smooth_va[i] = p[0] * Math.Pow(B[i], 2) + p[1] * B[i] + p[2];
            }

            List<double> va_diff = new List<double>();

            var va_diffTemp = diff(Va);
            foreach (double num in va_diffTemp)
            {
                va_diff.Add(num * 100 + 100);
            }
            va_diffTemp.Clear();

            int end_plateau = 0; // end_plateau is the va index of the end of the plateau
            for (int i = 0; i < va_diff.Count; i++)
            {
                if (va_diff[i] < 99.6)
                {
                    end_plateau = i;
                    break;
                }
            }

            if (end_plateau > 0)
            {
                end_plateau += 3;
            }
            // END SMOOTH

            // create lists for polyfit, and find minimum and index
            List<double> BPoly = new List<double>();
            List<double> vxPoly = new List<double>();

            // Define pulloff voltage as the difference between the minimum PSD voltage
            // and the PSD signal at zero deflection.
            double minimumValue = Vr.Min();
            int minimumValueIndex = Vr.IndexOf(Vr.Min());

            for (int i = ((int)spl + minimumValueIndex) / 2; i < (int)spl; i++)
            {
                BPoly.Add(B[i]);
                vxPoly.Add(Vr[i]);
            }

            var zero_polyfit = polyFit(BPoly, vxPoly, 1);
            var zero_defl_val = zero_polyfit[0] * B[minimumValueIndex] + zero_polyfit[1];
            var Vpo = zero_defl_val - minimumValue;

            // Fit line to the contact region of approach trace
            int slope_fit_end_idx = 0;
            for (int i = end_plateau; i < (int)spl; i++)
            {
                if (Va[i] <= (Va[end_plateau] + zero_defl_val) / 2)
                {
                    slope_fit_end_idx = i + end_plateau;
                    break;
                }
            }

            if (slope_fit_end_idx == end_plateau)
                slope_fit_end_idx += 10;

            BPoly.Clear();
            vxPoly.Clear();

            // fill lists before doing least squares
            for (int i = end_plateau; i < slope_fit_end_idx; i++)
            {
                BPoly.Add(B[i]);
                vxPoly.Add(Va[i]);
            }

            List<double> pV = new List<double>();

            try
            {
                pV = polyFit(BPoly, vxPoly, 1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // Outputs:
            // slope of the in-contact line (defl sens?) = -pV[0]
            // pull-off voltage = Vpo
            // pull-off deflection = -Vpo/pV[0]
            // max. in-contact deflection (nm) = -(vr[0] - zero_defl_val)/pV[0]
            
            PullOffV = Vpo;
            DeflSens = -pV[0];
            PullOffNM = -Vpo / pV[0];
            MaxDefl = -(Vr[0] - zero_defl_val) / pV[0];
        }

        public void Create_graph()
        {
            GraphModel graph = new GraphModel();

            //extract output path from input path
            string outputPath = Path.GetDirectoryName(inputPath) + @"\graphs\";
            string outputFilename = Path.GetFileName(inputPath) + ".png";
            string output = outputPath + outputFilename;

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            graph.Create_PullOffGraph(output, B, Va, Vr, "test");
        }

        public void Calc_friction(bool isTrimCI=false)
        {
            get_image_data(inputMStream);

            // clear our TMR data just in case
            TMR.Clear();
            double[][] trace = img_data[2];
            double[][] retrace = img_data[3];

            // old method - Measures WHOLE image TMR (bad!!!) //
            // faster non-parallel!
            //double sumTr = trace.SelectMany(x => x).ToList().Sum();
            //double sumRetr = retrace.SelectMany(x => x).ToList().Sum();

            //AvgFricTrV = sumTr / (spl * lineno);
            //AvgFricRetrV = sumRetr / (spl * lineno);

            //Stopwatch sw2 = new Stopwatch();
            //sw2.Start();
            //StdFricTrV = trace.SelectMany(x => x).ToList().StandardDeviation();
            //StdFricRetrV = retrace.SelectMany(x => x).ToList().StandardDeviation();
            //sw2.Stop();
            // old method end //

            // new method
            TraceLineAvg = new double[(int)lineno];
            RetraceLineAvg = new double[(int)lineno];
            LineAvg = new double[(int)lineno];
            LineAvgFricV = new double[(int)lineno];

            for (int i = 0; i < lineno; i++)
            {
                // same as bruker MultiMode 5 scope mode
                TraceLineAvg[i] = TraceData[i].Average();
                RetraceLineAvg[i] = RetraceData[i].Average();
                LineAvgFricV[i] = Math.Abs(TraceLineAvg[i] - RetraceLineAvg[i]) / 2;

                // this does pixel-pixel subtraction - not optimal!!!
                if (isTrimCI) // lets not waste precious CPU time
                {
                    var tmrQueue = new ConcurrentQueue<double>();
                    Parallel.For(0, (int)spl, j =>
                    {
                        var tmrTemp = Math.Abs(trace[j][i] - retrace[j][i]) / 2;
                        tmrQueue.Enqueue(tmrTemp);
                    });
                    TMR.AddRange(tmrQueue.ToList());
                }
            }
            
            // more pixel-pixel TMR
            if (isTrimCI)
            {
                var tmrNew = listTrimConfidenceInterval(TMR, 0.95);
                var tmrMeanNew = tmrNew.Mean();
                var tmrStdNew = tmrNew.StandardDeviation();

                AvgFricNewV = tmrMeanNew;
                StdevFricNewV = tmrStdNew;
            }
            else
            {
                AvgFricNewV = LineAvgFricV.Mean();
                StdevFricNewV = LineAvgFricV.StandardDeviation();
            }

            //Console.WriteLine("Time elapsed: {0:00.00} ms avg stdev calc", sw2.Elapsed.TotalMilliseconds / 2);
        }

        public int GetFileType(string inputFile)
        {
            // check if file exists
            if (!File.Exists(inputFile))
            {
                return 1000; // some high int so we never reach it below
            }
            // check for force or image file (or neither!)
            string line1;
            int fileType; // force=0, image=1, else 2
            using (StreamReader reader = new StreamReader(inputFile))
            {
                line1 = reader.ReadLine();
                if (line1 == @"\*Force file list") // force file
                {
                    fileType = 0;
                }
                else if (line1 == @"\*File list")
                {
                    fileType = 1;
                    //MessageBox.Show("Image files are not supported yet.", "Error");
                }
                else
                {
                    //MessageBox.Show("Corrupted or incorrect file selected.", "Error");
                    fileType = 2;
                }
            }
            return fileType;
        }

        private List<double> polyFitPlus(List<double> x, List<double> y, int n)
        {
            if (x.Count != y.Count)
            {
                return null; // both x/y must have equal sizes
            }

            MathNet.Numerics.Control.MaxDegreeOfParallelism = 1; // faster single core

            List<double> p = new List<double>();

            // do we need x=x(:) and y=y(:)? probably not - edit: sorted by just adding to a list sequentially

            // don't need nargout, only want one output (see carpick)
            Matrix X = new DenseMatrix(x.Count, 1 + n);
            Matrix Y = new DenseMatrix(y.Count, 1);

            // need vandermonde matrix like this:
            // [ x1^n , x1^n-1, ..., x1^0 ]
            // [ x2^n , x2^n-1, ..., x2^0 ]
            // [ ...  ,  ...  , ...,  ... ]
            // [ xn^n , xn^n-1, ..., xn^0 ]
            for (int i = 0; i < x.Count; i++)
            {
                for (int j = 0; j < 1 + n; j++)
                {
                    X.At(i, j, Math.Pow(x[i], n - j));
                }

                Y.At(i, 0, y[i]);
            }

            // solve least squares
            //var P = X.QR().Solve(Y);
            var P = X.Solve(Y); // Math.NET v3

            // convert to list
            for (int i = 0; i < P.RowCount; i++)
            {
                p.Add(P.At(i, 0));
            }

            return p;
        }

        private List<double> polyFit(List<double> x, List<double> y, int degree) //faster than Carpick, same results!
        {
            // Vandermonde matrix 
            // FROM ROSETTACODE.ORG http://rosettacode.org/wiki/Polynomial_regression#C.23
            var v = new DenseMatrix(x.Count, degree + 1);
            for (int i = 0; i < v.RowCount; i++)
                for (int j = 0; j <= degree; j++) v[i, j] = Math.Pow(x[i], j);
            var yv = new DenseVector(y.ToArray()).ToColumnMatrix();
            //QR qr = v.QR();
            // Math.Net doesn't have an "economy" QR, so:
            // cut R short to square upper triangle, then recompute Q
            //var r = qr.R.SubMatrix(0, degree + 1, 0, degree + 1);
            //var q = v.Multiply(r.Inverse());
            //var p = r.Inverse().Multiply(q.TransposeThisAndMultiply(yv));
            var p = v.Solve(yv);
            return p.Column(0).Reverse().ToList();
        }

        private List<int> di_header_find(MemoryStream ms, string find_string)
        {
            List<int> position = new List<int>();

            //preliminary variables
            bool header_end = false;
            bool eof = false;
            int counter = 1;

            // set our stream to the start
            ms.Position = 0;
            int byte_location = 0;

            CustomStreamReader sr = new CustomStreamReader(ms, Encoding.Default); // if encoding not set to Default, get ? symbols for º

            //primary loop
            while (!eof && !header_end)
            {
                // use custom streamreader class with readline that counts bytes read
                byte_location = sr.BytesRead;
                string line = sr.ReadLine();

                // check if contains the string of interest
                if (line.Contains(find_string))
                {
                    position.Add(byte_location);
                    counter += 1;
                }

                if (line == null) // reached end of file
                {
                    eof = true;
                }

                if (line.Contains(@"\*File list end")) // check for header end
                {
                    header_end = true;
                }
            }

            return position;
        }

        private void get_image_data(MemoryStream ms)
        {
            // for NEW nanoscope:
            // channel 1 - height
            // channel 2 - deflection error
            // channel 3 - friction (trace)
            // channel 4 - friction (retrace)

            scal_data = di_header_find(inputMStream, @"\@2:Z scale: V [Sens.");
            //pos_spl = di_header_find(inputMStream, @"\Samps");
            //pos_data = di_header_find(inputMStream, @"\Data offset");
            file_type_data = di_header_find(inputMStream, @"Image Data:");
            pos_lineno = di_header_find(inputMStream, @"\Number of lines");

            int L = pos_data.Count();

            spl = extract_num(inputMStream, pos_spl[0]);
            lineno = extract_num(inputMStream, pos_lineno[0]);

            // fix to 4 channels (temp)
            if (L > 4)
            {
                L = 4;
            }

            // initialize array
            for (int i = 0; i < L; i++)
            {
                img_data[i] = new double[(int)spl][];
                for (int x = 0; x < (int)spl; x++)
                {
                    img_data[i][x] = new double[(int)lineno];
                }
            }

            // now go through all channels and extract image data
            for (int i = 0; i < L; i++)
            {
                int img_pos = (int)extract_num(inputMStream, pos_data[i]);
                scaling = extract_num(inputMStream, scal_data[i], 1);

                BinaryReader br = new BinaryReader(inputMStream);
                inputMStream.Position = img_pos;

                // read data into our array... X data is sequential, and after $spl the next line starts
                for (int y = 0; y < (int)lineno; y++)
                {
                    double[] fricData = new double[(int)spl];
                    for (int x = 0; x < (int)spl; x++)
                    {
                        double data = (double)br.ReadInt16();
                        img_data[i][x][y] = data * scaling;

                        // do our (re)trace stuff too!
                        fricData[x] = data * scaling;
                    }

                    if (i == 2) // trace
                    {
                        TraceData.Add(fricData);
                    }
                    else if (i == 3) // retrace
                    {
                        RetraceData.Add(fricData);
                    }
                }
            }
        }

        private double extract_num(MemoryStream ms, int streamPosition, int number_index = 0)
        {
            double n;

            StreamReader sr = new StreamReader(inputMStream, Encoding.Default);

            ms.Position = streamPosition;
            string line = sr.ReadLine();

            // extract decimals
            string[] doubleArray = Regex.Split(line, @"[^0-9\.]+");
            string digits = "";

            for (int i = 0; i < doubleArray.Length; i++)
            {
                double x;
                if (double.TryParse(doubleArray[i], out x))
                {
                    if (number_index == 0)
                    {
                        digits = doubleArray[i]; // first digit always null!
                        break;
                    }
                    number_index -= 1; // reduce index number by 1, until we reach 0 (number wanted), not elegant but it works
                }
            }

            n = Convert.ToDouble(digits);

            return n;
        }

        private List<double> diff(List<double> list)
        {
            // DIFF(X), for a vector X, is [X(2)-X(1)  X(3)-X(2) ... X(n)-X(n-1)].
            int count = list.Count;

            if (count < 2) // need at least 2 numbers to subtract from each other!
            {
                return null;
            }

            List<double> diff_list = new List<double>();

            for (int i = 1; i < count; i++)
            {
                double x = list[i] - list[i - 1];
                diff_list.Add(x);
            }

            return diff_list;
        }

        private MemoryStream loadFileToMemoryStream(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                MemoryStream ms = new MemoryStream();
                ms.SetLength(fs.Length);
                fs.Read(ms.GetBuffer(), 0, (int)fs.Length);
                ms.Position = 0;
                return ms;
            }
        }

        private List<double> listTrimConfidenceInterval(List<double> list, double interval=0.95)
        {
            var mean = list.Mean();
            var stdev = list.StandardDeviation();
            var confidenceInterval = 0.95;
            var theta = (confidenceInterval + 1) / 2;
            var T = Normal.InvCDF(0, 1, theta);
            //var t = T * (stdev / Math.Sqrt(spl * lineno));
            var t = T * stdev;
            var listNew = list.OrderByDescending(x => x).TakeWhile(x => x >= mean - t).Reverse().TakeWhile(x => x <= mean + t).ToList();

            return listNew;
        }

        // Explicit predicate delegate.
        private static bool findLessThan(double n, double target)
        {
            if (n < target)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}