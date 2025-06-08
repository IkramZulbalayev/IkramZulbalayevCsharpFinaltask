using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;

class Master
{
    static ConcurrentDictionary<string, int> globalIndex = new();

    static void Main()
    {
        // Firstly With Just One Pipe
        ListenToScanner("pipe_scannerA");  

        Console.WriteLine("\nFinal Aggregated Result:");
        foreach (var entry in globalIndex)
            Console.WriteLine($"{entry.Key}:{entry.Value}");
    }

    static void ListenToScanner(string pipeName)
    {
        using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);
        Console.WriteLine("[Master] Waiting for scanner connection on " + pipeName + "...");
        pipeServer.WaitForConnection();
        Console.WriteLine("[Master] Connected to scanner on " + pipeName + ".");

        using var reader = new StreamReader(pipeServer, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line == "__END__")
                break;

            var parts = line.Split(':');
            if (parts.Length != 3)
                continue;

            string key = $"{parts[0]}:{parts[1]}";
            if (!int.TryParse(parts[2], out int count))
                continue;

            if (globalIndex.ContainsKey(key))
                globalIndex[key] += count;
            else
                globalIndex[key] = count;
        }

        Console.WriteLine("[Master] Scanner on " + pipeName + " finished.");
    }
}
