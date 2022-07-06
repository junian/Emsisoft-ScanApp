using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ScanApp.Services
{
    public class FileHashService
    {
        public FileHashService()
        {
        }

        private static string GenerateHashString(HashAlgorithm algo, string text)
        {
            // Compute hash from text parameter
            algo.ComputeHash(Encoding.UTF8.GetBytes(text));

            // Get has value in array of bytes
            var result = algo.Hash;

            // Return as hexadecimal string
            return string.Join(
                string.Empty,
                result.Select(x => x.ToString("x2")));
        }

        private string GenerateHashString(HashAlgorithm algo, Stream stream)
        {
            // Compute hash from text parameter
            algo.ComputeHash(stream);

            // Get has value in array of bytes
            var result = algo.Hash;

            // Return as hexadecimal string
            return string.Join(
                string.Empty,
                result.Select(x => x.ToString("x2")));
        }

        public string MD5(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return MD5(stream);
            }
        }

        public string MD5(Stream stream)
        {
            var result = default(string);

            using (var algo = new MD5CryptoServiceProvider())
            {
                result = GenerateHashString(algo, stream);
            }

            return result;
        }

        public string SHA1(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return SHA1(stream);
            }
        }

        public string SHA1(Stream stream)
        {
            var result = default(string);

            using (var algo = new SHA1Managed())
            {
                result = GenerateHashString(algo, stream);
            }

            return result;
        }

        public string SHA256(string filePath)
        {
            using(var stream = File.OpenRead(filePath))
            {
                return SHA256(stream);
            }
        }

        public string SHA256(Stream stream)
        {
            var result = default(string);

            using (var algo = new SHA256Managed())
            {
                result = GenerateHashString(algo, stream);
            }

            return result;
        }

        public string SHA256Content(string text)
        {
            var result = default(string);

            using (var algo = new SHA256Managed())
            {
                result = GenerateHashString(algo, text);
            }

            return result;
        }

    }
}

