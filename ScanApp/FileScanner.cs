using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ScanApp.Models;
using ScanApp.Services;
using Serilog;

namespace ScanApp
{
    public class FileScanner
    {
        private ILogger _log;
        private FileHashService _hashService;

        public FileScanner()
        {
            _log = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            _hashService = new FileHashService();
        }

        public FileScannerReport StartScan(string rootPath)
        {

            var report = new FileScannerReport {};

            var files = System.IO.Directory.GetFiles(
                rootPath,
                "*",
                System.IO.SearchOption.AllDirectories);

            report.TotalFiles = files.LongLength;

            Parallel.ForEach(files, file =>
            {
                //ClearLastLine();
                //Console.WriteLine($"Processing {file}");

                Process(report, file);    
            });

            return report;
        }

        public async Task<FileScannerReport> StartScanAsync(string rootPath)
        {
            var fileList = new List<BlockingCollection<string>>();

            var maxThreads = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;
            var iterator = 0;

            for(var i=0; i < maxThreads; i++)
            {
                fileList.Add(new BlockingCollection<string> { });
            }

            var report = new FileScannerReport { };

            var processAction = new Action<BlockingCollection<string>>((collection) =>
            {
                while (!collection.IsCompleted)
                {
                    var data = default(string);

                    try
                    {
                        data = collection.Take();
                    }
                    catch (InvalidOperationException) { }

                    if (data != null)
                    {
                        Process(report, data);
                    }
                }
            });

            var gatheringFilesAction = new Action(() =>
            {
                var queue = new Queue<string>();
                queue.Enqueue(rootPath);

                while (queue.Count > 0)
                {
                    var currentDir = queue.Dequeue();

                    var files = Directory.GetFiles(currentDir);

                    report.TotalFiles += files.LongLength;

                    foreach(var file in files)
                    {
                        fileList[iterator].Add(file);
                        iterator = (iterator + 1) % maxThreads;
                        //Console.WriteLine(iterator);
                    }

                    var dirs = Directory.GetDirectories(currentDir);
                    foreach(var dir in dirs)
                    {
                        queue.Enqueue(dir);
                    }

                }

                for (var i = 0; i < maxThreads; i++)
                {
                    fileList[i].CompleteAdding();
                }
                
            });

            var taskList = new List<Task> { };
            for(var i=0; i<maxThreads; i++)
            {
                var collection = fileList[i];

                taskList.Add(Task.Run(() => processAction(collection)));
            }

            taskList.Add(Task.Run(gatheringFilesAction));

            await Task.WhenAll(taskList);

            return report;
        }

        private void Process(FileScannerReport report, string file)
        {
            var fileHash = new FileHash
            {
                FilePath = file,
                FileSize = new FileInfo(file).Length,
            };

            report.FileHashList.Add(fileHash);

            try
            {
                fileHash.MD5 = _hashService.MD5(file);
                fileHash.SHA1 = _hashService.SHA1(file);
                fileHash.SHA256 = _hashService.SHA256(file);
            }
            catch (Exception ex)
            {
                report.TotalErrors += 1;
                fileHash.IsError = true;
                fileHash.ErrorMessage = ex.ToString();
                _log.Error(ex.ToString());
            }
        }

        public void ClearLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
    }
}
