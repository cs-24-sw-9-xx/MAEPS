using System;
using System.IO;

namespace Maes.Statistics.Csv
{
    public interface ICsvData
    {
        void WriteHeader(StreamWriter streamWriter, char delimiter);

        void WriteRow(StreamWriter streamWriter, char delimiter);

        ReadOnlySpan<string> ReadRow(ReadOnlySpan<string> columns);
    }
}