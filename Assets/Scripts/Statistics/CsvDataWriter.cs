using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using CsvHelper;
using CsvHelper.Configuration;

using UnityEngine;

namespace Maes.Statistics.Writer
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
            });

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

            _finished = true;
        }

        public void Dispose()
        {
            if (!_finished)
            {
                // This can happen because of unity domain reload weirdness
                if (_csvWriter == null)
                {
                    return;
                }

                try
                {
                    _csvWriter.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    Debug.Log("CSVDataWriter: ignoring object disposed exception. Probably due to unity domain reload weirdness!");
                }

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