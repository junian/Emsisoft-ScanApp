using System;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace ScanApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            var errorMessage = string.Empty;

            if (args.Length < 1)
            {
                errorMessage = "[FolderPath] is required.";
                Console.WriteLine($"ERR: {errorMessage}");
                log.Error(errorMessage);
                return;
            }

            var folderPath = args[0];
            if (!Directory.Exists(folderPath))
            {
                errorMessage = "[FolderPath] doesn't exist.";
                Console.WriteLine($"ERR: {errorMessage}");
                log.Error(errorMessage);
                return;
            }

            log.Information($"Start scanning files ...");

            var timer = new Stopwatch();
            timer.Start();
            
            var fileScanner = new FileScanner();

            var task = fileScanner.StartScanAsync(folderPath);
            task.Wait();

            var result = task.Result;

            /*
            var result = fileScanner.StartScan(folderPath);
            */

            timer.Stop();
            Console.WriteLine($"Total File(s)     : {result.TotalFiles}");
            // Console.WriteLine($"Total Error(s)    : {result.TotalErrors}");
            Console.WriteLine($"Total Time        : {timer.Elapsed.ToString()}");

            log.Information($"File scanning finished in {timer.Elapsed.ToString()}.");
        }
    }
}

