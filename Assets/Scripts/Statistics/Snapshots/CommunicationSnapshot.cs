using System;
using System.Globalization;
using System.IO;

using Maes.Statistics.Csv;

namespace Maes.Statistics.Snapshots
{
    public struct CommunicationSnapshot : ICsvData
    {
        public int Tick { get; private set; }

        public int ReceivedMessageCount { get; private set; }

        public int SentMessageCount { get; private set; }

        public CommunicationSnapshot(int tick, int receivedMessageCount, int sentMessageCount)
        {
            Tick = tick;
            ReceivedMessageCount = receivedMessageCount;
            SentMessageCount = sentMessageCount;
        }

        public void WriteHeader(StreamWriter streamWriter, char delimiter)
        {
            streamWriter.Write(nameof(Tick));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(ReceivedMessageCount));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(SentMessageCount));
        }

        public void WriteRow(StreamWriter streamWriter, char delimiter)
        {
            streamWriter.Write(Tick);
            streamWriter.Write(delimiter);
            streamWriter.Write(ReceivedMessageCount);
            streamWriter.Write(delimiter);
            streamWriter.Write(SentMessageCount);
        }

        public ReadOnlySpan<string> ReadRow(ReadOnlySpan<string> columns)
        {
            Tick = Convert.ToInt32(columns[0], CultureInfo.InvariantCulture);
            ReceivedMessageCount = Convert.ToInt32(columns[1], CultureInfo.InvariantCulture);
            SentMessageCount = Convert.ToInt32(columns[2], CultureInfo.InvariantCulture);

            return columns[3..];
        }
    }
}