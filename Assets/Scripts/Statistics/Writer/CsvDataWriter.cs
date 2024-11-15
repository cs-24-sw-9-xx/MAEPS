using System.Collections.Generic;
using System.Globalization;
using System.IO;

using CsvHelper;
using CsvHelper.Configuration;

using UnityEngine;

namespace Maes.Statistics.Writer
{
    public abstract class CsvDataWriter<TSnapShot>
    {
        private readonly List<TSnapShot> _snapShots;
        private readonly string _path;

        protected CsvDataWriter(List<TSnapShot> snapShots, string filename)
        {
            _snapShots = snapShots;
            _path = filename + ".csv";
        }

        public void CreateCsvFile(string separator)
        {
            using var writer = new StreamWriter(Path.GetFullPath(_path));
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = separator
            });

            csv.WriteHeader<TSnapShot>();
            csv.NextRecord();
            foreach (var snapShot in _snapShots)
            {
                PrepareSnapShot(snapShot);
                csv.WriteRecord(snapShot);
                csv.NextRecord();
            }
            Debug.Log($"Writing statistics to path: {_path}");
        }

        protected virtual void PrepareSnapShot(TSnapShot snapShot) { }
    }
}