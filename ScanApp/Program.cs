using System;
using System.Diagnostics;
using System.IO;

namespace ScanApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERR: [FolderPath] is required.");
                return;
            }

            var folderPath = args[0];
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("ERR: [FolderPath] doesn't exist.");
                return;
            }

            var timer = new Stopwatch();
            timer.Start();
            
            var fileScanner = new FileScanner();
            var task = fileScanner.StartScanAsync(folderPath);
            task.Wait();
            var result = task.Result;

            timer.Stop();
            Console.WriteLine($"Total File(s)     : {result.TotalFiles}");
            Console.WriteLine($"Total Error(s)    : {result.TotalErrors}");
            Console.WriteLine($"Total Time        : {timer.Elapsed.ToString()}");
        }
    }
}

