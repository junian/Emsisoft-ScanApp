using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ScanApp.Models;
using ScanApp.Services;

namespace ScanApp
{
    public class FileScanner
    {
        public FileScanner()
        {
        }

        public async Task<FileScannerReport> StartScanAsync(string rootPath)
        {
            var hashService = new FileHashService();

            var report = new FileScannerReport {};

            var files = System.IO.Directory.GetFiles(
                rootPath,
                "*",
                System.IO.SearchOption.AllDirectories);

            report.TotalFiles = files.LongLength;

            Parallel.ForEach(files, file =>
            {
                var fileHash = new FileHash
                {
                    FilePath = file,
                };

                report.FileHashList.Add(fileHash);

                try
                {
                    fileHash.MD5 = hashService.MD5(file);
                    fileHash.SHA1 = hashService.SHA1(file);
                    fileHash.SHA256 = hashService.SHA256(file);
                }
                catch (Exception ex)
                {
                    report.TotalErrors++;
                    fileHash.IsError = true;
                    fileHash.ErrorMessage = ex.ToString();
                }
            });

            return report;
        }
    }
}

