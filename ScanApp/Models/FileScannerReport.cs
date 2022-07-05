using System;
using System.Collections.Generic;

namespace ScanApp.Models
{
    public class FileScannerReport
    {
        public FileScannerReport()
        {
            TotalFiles = 0;
            TotalErrors = 0;
            FileHashList = new List<FileHash> {};
        }

        public long TotalFiles { get; set; }
        public long TotalErrors { get; set; }

        public IList<FileHash> FileHashList { get; set; }
    }
}

