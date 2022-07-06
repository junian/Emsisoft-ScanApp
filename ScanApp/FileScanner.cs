using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            using(var db = new AppDbContext())
            {
                await db.Database.EnsureCreatedAsync();
            }

            var fileList = new List<BlockingCollection<string>>();

            var maxThreads = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;
            var threadsInProcess = maxThreads;
            var iterator = 0;

            for(var i=0; i < maxThreads; i++)
            {
                fileList.Add(new BlockingCollection<string> { });
            }

            var report = new FileScannerReport { };

            var dbQueue = new BlockingCollection<FileHash> { };

            // Calculate Hash for each file.
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

                Interlocked.Decrement(ref threadsInProcess);
                if (threadsInProcess <= 0)
                    dbQueue.CompleteAdding();
            });

            // Get all files
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

        private async void Process(FileScannerReport report, string file, BlockingCollection<FileHash> dbQueue = null)
        {
            using (var db = new AppDbContext())
            {
                var fileHash = new FileHash
                {
                    FilePath = file,
                    FileSize = new FileInfo(file).Length,
                    CacheKey = _hashService.SHA256Content(file),
                };

                var currentResult = await db.ScanResults.FirstOrDefaultAsync(x => x.CacheKey == fileHash.CacheKey);

                var skip = false;

                if(currentResult != null)
                {
                    // If same file path and file size and exist in database, just skip and do increment
                    if (fileHash.FilePath == currentResult.FilePath && fileHash.FileSize == currentResult.FileSize)
                    {
                        _log.Information($"Skipping [{file}] because already scanned.");
                        currentResult.LastSeen = DateTime.Now;
                        currentResult.Scanned += 1;

                        skip = true;
                    }
                    // Same path but different file size, could be different file.
                    else
                    {
                        db.ScanResults.Remove(currentResult);
                    }
                }

                await db.SaveChangesAsync();

                if (skip)
                    return;

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

            
                await db.ScanResults.AddAsync(new ScanResult
                {
                    MD5 = fileHash.MD5,
                    Sha1 = fileHash.SHA1,
                    Sha256 = fileHash.SHA256,
                    FilePath = fileHash.FilePath,
                    FileSize = fileHash.FileSize,
                    ErrorMessage = fileHash.ErrorMessage,
                    IsError = fileHash.IsError,
                    Scanned = 1,
                    LastSeen = DateTime.Now,
                    CacheKey = _hashService.SHA256Content(fileHash.FilePath),
                });

                await db.SaveChangesAsync();
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
