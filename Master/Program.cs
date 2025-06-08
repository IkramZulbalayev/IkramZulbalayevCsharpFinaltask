using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

class Master
{
    static ConcurrentDictionary<string, int> globalIndex = new();

    static void Main()
    {
        //Set the process to run only on CPU core 0
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 0);

        // Using MultiThreading for Two Pipes
        Thread scannerAThread = new(() => ListenToScanner("pipe_scannerA"));
        Thread scannerBThread = new(() => ListenToScanner("pipe_scannerB"));

        scannerAThread.Start();
        scannerBThread.Start();

        scannerAThread.Join();
        scannerBThread.Join();

        Console.WriteLine("\nFinal Aggregated Result:");
        foreach (var entry in globalIndex)
        {
            Console.WriteLine($"{entry.Key}:{entry.Value}");
        }
            
    }

    static void ListenToScanner(string pipeName)
    {
        try
        {
            // Create a named pipe server to listen for incoming connections from the scanner
            using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);

            Console.WriteLine("[Master] Waiting for scanner connection on " + pipeName + "...");

            // Wait for the scanner to connect to this named pipe
            pipeServer.WaitForConnection();
            Console.WriteLine("[Master] Connected to scanner on " + pipeName + ".");

            // Use a StreamReader to read text data from the pipe
            using var reader = new StreamReader(pipeServer, Encoding.UTF8);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line == "__END__")
                    break;

                var parts = line.Split(':');
                if (parts.Length != 3)
                {
                    Console.WriteLine("[Master] Skipping malformed line in input: " + line);
                    continue;
                }

                // Construct a unique key using filename and word
                string key = $"{parts[0]}:{parts[1]}";
                if (!int.TryParse(parts[2], out int count))
                {
                    Console.WriteLine("[Master] invalid quantity skipped: " + line);
                    continue;
                }

                globalIndex.AddOrUpdate(key, count, (k, existingCount) => existingCount + count);

            }

            Console.WriteLine("[Master] Scanner on " + pipeName + " finished.");

        } catch (Exception e) 
        {
            Console.WriteLine("[Master] Error on pipe " + pipeName + ": " + e.Message);
        }
        
    }
}
