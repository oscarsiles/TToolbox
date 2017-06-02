using LumenWorks.Framework.IO.Csv;
using System;
using System.Data;
using System.IO;

namespace TToolbox
{
    // using CsvReader by LumenWorks
    internal class CSV
    {
        public DataSet ReadCSV(string path, bool includeHeaders)
        {
            DataSet ds = new DataSet();
            using (CsvReader csv = new CsvReader(new StreamReader(path), includeHeaders))
            {
                try
                {
                    ds.Load(csv, LoadOption.Upsert, "");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return ds;
        }

        public string ReadFieldCSV(string path, int row, int col, bool includeHeaders) // will only read a single field, so don't use if using multiple fields!
        {
            string str = "";

            var ds = ReadCSV(path, includeHeaders);

            try
            {
                str = Convert.ToString(ds.Tables[0].Rows[row].ItemArray[col]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return str;
        }
    }
}