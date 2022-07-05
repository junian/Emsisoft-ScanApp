using System;
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


        }
    }
}

