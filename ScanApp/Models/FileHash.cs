using System;
namespace ScanApp.Models
{
    public class FileHash
    {
        public FileHash()
        {
        }

        public string FilePath { get; set; }
        public string MD5 { get; set; }
        public string SHA1 { get; set; }
        public string SHA256 { get; set; }

    }
}
