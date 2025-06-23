using System.IO;

namespace Maes.Statistics.Csv
{
    public interface ICsvData
    {
        void WriteHeader(StreamWriter streamWriter, char delimiter);

        void WriteRow(StreamWriter streamWriter, char delimiter);

#if NET9_0_OR_GREATER
        void ReadRow(ReadOnlySpan<char> columns, ref MemoryExtensions.SpanSplitEnumerator<char> enumerator);
#endif
    }
}