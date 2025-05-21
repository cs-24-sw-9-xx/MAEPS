using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using CsvHelper;
using CsvHelper.Configuration;

using UnityEngine;

namespace Maes.Statistics
{
    public sealed class CsvDataWriter<TSnapShot> : IDisposable
    {
        private readonly string _tempPath;
        private readonly string _path;

        private readonly StreamWriter _streamWriter;
        private readonly CsvWriter _csvWriter;

        private bool _finished;

        public CsvDataWriter(string filename, string delimiter = ",")
        {
            _tempPath = $"{filename}.INCOMPLETE";
            _path = $"{filename}.csv";

            _streamWriter = new StreamWriter(_tempPath);
            _csvWriter = new CsvWriter(_streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter
            }, leaveOpen: true);

            // Write the header immediately
            _csvWriter.WriteHeader<TSnapShot>();
            _csvWriter.NextRecord();
        }

        public void AddRecord(TSnapShot record)
        {
            _csvWriter.WriteRecord(record);
            _csvWriter.NextRecord();
        }

        public void AddRecords(IEnumerable<TSnapShot> records)
        {
            _csvWriter.WriteRecords(records);
        }

        public void Finish()
        {
            _csvWriter.Dispose();

            // TOCTOU problem
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(_tempPath, _path);

            Debug.LogFormat("Wrote statistics to {0}", _path);

            _finished = true;
        }

        public void Dispose()
        {
            if (!_finished)
            {
                _csvWriter.Dispose();
                _streamWriter.Dispose();

                File.Delete(_tempPath);
            }

            GC.SuppressFinalize(this);
        }

        ~CsvDataWriter()
        {
            Dispose();
        }
    }
}