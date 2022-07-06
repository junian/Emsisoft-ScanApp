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

        // Old function before database.
        public FileScannerReport StartScan(string rootPath)
        {

            var report = new FileScannerReport {};

            var files = System.IO.Directory.GetFiles(
                rootPath,
                "*",
                System.IO.SearchOption.AllDirectories);

            report.TotalFiles = files.LongLength;

            Parallel.ForEach(files, async (file) =>
            {
                await ProcessAsync(report, file);    
            });

            return report;
        }

        public async Task<FileScannerReport> StartScanAsync(string rootPath)
        {
            // Make sure database is created if not.
            using(var db = new AppDbContext())
            {
                await db.Database.EnsureCreatedAsync();
            }

            var fileList = new List<BlockingCollection<string>>();

            var maxThreads = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;
            var iterator = 0;

            for(var i=0; i < maxThreads; i++)
            {
                fileList.Add(new BlockingCollection<string> { });
            }

            var report = new FileScannerReport { };

            // This thread is responsible to process file hashing.
            // And also to update the result in database.
            var processAction = new Func<BlockingCollection<string>, Task>(async (collection) =>
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
                        await ProcessAsync(report, data);
                    }
                }
            });

            // This thread is responsible to scan all files in directory and sub-directories.
            // Send the file list to processAction to be processed.
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

        private async Task ProcessAsync(FileScannerReport report, string file)
        {
            using (var db = new AppDbContext())
            {
                var fileHash = new FileHash
                {
                    FilePath = file,
                    FileSize = new FileInfo(file).Length,
                    CacheKey = _hashService.SHA256Content(file),
                };

                // Let's see if it's scanned in the past.
                var currentResult = await db.ScanResults.FirstOrDefaultAsync(x => x.CacheKey == fileHash.CacheKey);

                var skip = false;

                // File already scanned before, so let's check it before re-hash.
                if(currentResult != null)
                {
                    // If same file path and file size and exist in database, just skip and do increment.
                    if (fileHash.FilePath == currentResult.FilePath && fileHash.FileSize == currentResult.FileSize)
                    {
                        _log.Information($"Skipping [{file}] because already scanned.");
                        currentResult.LastSeen = DateTime.Now;
                        currentResult.Scanned += 1;

                        skip = true;
                    }
                    // Same path but different file size, could be different file.
                    // Let's remove it from DB and re-hash the file.
                    else
                    {
                        db.ScanResults.Remove(currentResult);
                    }
                }

                await db.SaveChangesAsync();

                if (skip)
                    return;

                report.FileHashList.Add(fileHash);

                // Calculate file hashes
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

                // Insert scan result to database.
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
    }
}
