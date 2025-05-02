using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

public static class FileLogger
{
    private static readonly BlockingCollection<(string FilePath, string Message)> LogQueue = new BlockingCollection<(string, string)>();

    static FileLogger()
    {
        var loggingThread = new Thread(ProcessLogQueue) { IsBackground = true };
        loggingThread.Start();
    }

    public static void LogToFile(string filePath, string message)
    {
        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // Add the log entry to the queue
        LogQueue.Add((filePath, message));
    }

    private static void ProcessLogQueue()
    {
        foreach (var (filePath, message) in LogQueue.GetConsumingEnumerable())
        {
            try
            {
                using (var writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(message);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Failed to write log to {filePath}: {ex.Message}");
            }
        }
    }
}