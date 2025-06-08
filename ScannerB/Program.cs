using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;

class ScannerB
{
    static ConcurrentQueue<string> fileQueue = new();
    static ConcurrentQueue<string> sendQueue = new();

    static void Main()
    {
        //Asking to enter the Directory
        Console.Write("Enter directory path: ");
        string? inputPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(inputPath) || !Directory.Exists(inputPath))
        {
            Console.WriteLine("Invalid directory path.");
            return;
        }

        var allFiles = Directory.GetFiles(inputPath, "*.txt");
        var scannerBFiles = allFiles.Where((file, index) => index % 2 != 0);

        foreach (var file in scannerBFiles)
            fileQueue.Enqueue(file);

        // Start reading and sending threads
        Thread readerThread = new(ReadFiles);
        Thread senderThread = new(SendToMaster);

        readerThread.Start();
        senderThread.Start();

        readerThread.Join();
        senderThread.Join();
    }

    // Reads each file, counts word occurrences, and adds results to the queue
    static void ReadFiles()
    {
        while (fileQueue.TryDequeue(out var file))
        {
            string content = File.ReadAllText(file).ToLower();
            var wordMatches = Regex.Matches(content, @"\b\w+\b");

            var wordCounts = new Dictionary<string, int>();
            foreach (Match match in wordMatches)
            {
                string word = match.Value;
                if (!wordCounts.ContainsKey(word))
                    wordCounts[word] = 0;
                wordCounts[word]++;
            }

            foreach (var kv in wordCounts)
                sendQueue.Enqueue($"{Path.GetFileName(file)}:{kv.Key}:{kv.Value}");
        }

        // Signal that scanning is done
        sendQueue.Enqueue("__END__");
    }

    // Sends results to the master process through named pipe
    static void SendToMaster()
    {
        using var pipe = new NamedPipeClientStream(".", "pipe_scannerB", PipeDirection.Out);
        pipe.Connect();
        using var writer = new StreamWriter(pipe) { AutoFlush = true };

        while (true)
        {
            if (sendQueue.TryDequeue(out var line))
            {
                writer.WriteLine(line);
                if (line == "__END__")
                    break;
            }
            else
            {
                Thread.Sleep(10);
            }
        }
    }
}
