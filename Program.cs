using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Drive Detection ===");
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.IsReady)
                {
                    Console.WriteLine($"Name: {drive.Name}");
                    Console.WriteLine($"Type: {drive.DriveType}");
                    Console.WriteLine($"Format: {drive.DriveFormat}");
                    Console.WriteLine($"Total size: {drive.TotalSize / 1_000_000_000.0:F2} GB");
                    Console.WriteLine($"Free space: {drive.AvailableFreeSpace / 1_000_000_000.0:F2} GB");
                    Console.WriteLine();
                }
            }
            catch
            {
                // Some virtual/mount drives may throw exceptions on Linux
            }
        }

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            Console.WriteLine($"\n=== Benchmarking Drive: {drive.Name} ===");

            string testDir = Path.Combine(drive.RootDirectory.FullName, "IoBenchmarkTest");
            Directory.CreateDirectory(testDir);
            RunBenchmark(testDir);
            Directory.Delete(testDir, true);
        }
        
        static void RunBenchmark(string path)
        {
            long[] sizes = { 10_000, 1_000_000, 100_000_000 }; // 10KB, 1MB, 100MB
            long[] filecounts  = { 1000, 100, 10};

            for (int i = 0; i<3; i++)
            {
                Console.WriteLine($"\n--- Testing {filecounts[i]} x {sizes[i] / 1000} KB files ---");

                // WRITE TEST
                var sw = Stopwatch.StartNew();
                for (int j = 0; j < filecounts[i]; j++)
                {
                    string file = Path.Combine(path, $"file_{sizes[i]}_{j}.bin");
                    byte[] data = new byte[sizes[i]];
                    RandomNumberGenerator.Fill(data);
                    File.WriteAllBytes(file, data);
                }
                sw.Stop();
                double writeTime = sw.Elapsed.TotalSeconds;
                Console.WriteLine($"Write: {filecounts[i]} files in {writeTime:F2}s, {(filecounts[i] / writeTime):F1} IOPS, {(filecounts[i] * sizes[i] / writeTime / 1_000_000):F1} MB/s");

                // READ TEST
                sw.Restart();
                foreach (var file in Directory.GetFiles(path, $"file_{sizes[i]}_*.bin"))
                    File.ReadAllBytes(file);
                sw.Stop();
                double readTime = sw.Elapsed.TotalSeconds;
                Console.WriteLine($"Read:  {filecounts[i]} files in {readTime:F2}s, {(filecounts[i] / readTime):F1} IOPS, {(filecounts[i] * sizes[i] / readTime / 1_000_000):F1} MB/s");
                
                // Cleanup
                foreach (var file in Directory.GetFiles(path, $"file_{sizes[i]}_*.bin"))
                    File.Delete(file);
        
            }
        }
        // --- Basic CPU / Memory metrics ---
        using var proc = Process.GetCurrentProcess();
        Console.WriteLine("\n=== Resource Usage ===");
        Console.WriteLine($"CPU Time Used: {proc.TotalProcessorTime.TotalSeconds:F2}s");
        Console.WriteLine($"Peak Memory: {proc.PeakWorkingSet64 / 1024 / 1024:F1} MB");

        Console.WriteLine("\nBenchmark Completed");
    }
}
