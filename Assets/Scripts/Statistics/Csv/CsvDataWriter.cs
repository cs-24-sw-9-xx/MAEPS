using System;
using System.Globalization;
using System.IO;

namespace Maes.Statistics.Csv
{
    public sealed class CsvDataWriter<TSnapShot> : IDisposable
    where TSnapShot : ICsvData, new()
    {
        private readonly char _delimiter;
        private readonly string _tempPath;
        private readonly string _path;

        private readonly InvariantStreamWriter _streamWriter;

        private bool _finished;

        public CsvDataWriter(string filename, char delimiter = ';')
        {
            _delimiter = delimiter;
            _tempPath = $"{filename}.INCOMPLETE";
            _path = $"{filename}.csv";

            _streamWriter = new InvariantStreamWriter(_tempPath);

            new TSnapShot().WriteHeader(_streamWriter, _delimiter);
            _streamWriter.Write('\n');
        }

        public void AddRecord(TSnapShot record)
        {
            record.WriteRow(_streamWriter, _delimiter);
            _streamWriter.Write('\n');
        }

        public void Finish()
        {
            _streamWriter.Close();

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
                _streamWriter.Dispose();

                File.Delete(_tempPath);
            }

            GC.SuppressFinalize(this);
        }

        ~CsvDataWriter()
        {
            Dispose();
        }

        private sealed class InvariantStreamWriter : StreamWriter
        {
            public InvariantStreamWriter(string path) : base(path)
            {
            }

            public override IFormatProvider FormatProvider => CultureInfo.InvariantCulture;
        }
    }
}