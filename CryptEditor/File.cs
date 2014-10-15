using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace CryptEditor
{
    public sealed class File : INotifyPropertyChanged
    {
        private const int iterations = 30000;

        public static readonly File Null = new File(string.Empty);
        private string name;

        public event PropertyChangedEventHandler PropertyChanged;

        public File(FileInfo file)
        {
            Name = Path.ChangeExtension(file.Name, null);
            FullName = file.FullName;
        }

        public File(string name)
        {
            Name = name;
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;

                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public string FullName { get; set; }

        public string Load(string password)
        {
            if (string.IsNullOrEmpty(FullName))
                return string.Empty;
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            return DecryptData(System.IO.File.ReadAllBytes(FullName), password);
        }

        public void Save(string password, string contents)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");
            if (string.IsNullOrEmpty(FullName))
                throw new InvalidOperationException("FullName can't be empty.");

            var salt = GenerateSalt();
            var encrypted = EncryptData(salt, password, contents);
            var hash = ComputeHash(encrypted);

            using (var stream = System.IO.File.Create(FullName))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(salt);
                writer.Write(hash);
                writer.Write(encrypted);
                writer.Flush();
            }
        }

        private static string DecryptData(byte[] data, string password)
        {
            var salt = new byte[32];
            var hash = new byte[32];
            var encrypted = new byte[data.Length - 64];
            Array.Copy(data, 0, salt, 0, 32);
            Array.Copy(data, 32, hash, 0, 32);
            Array.Copy(data, 64, encrypted, 0, encrypted.Length);

            // Verify hash
            if (!((IStructuralEquatable)hash).Equals(ComputeHash(encrypted), StructuralComparisons.StructuralEqualityComparer))
                throw new InvalidOperationException("Hashes do not match, possible corrupted file.");

            // Decrypt
            using (var rfc2898 = new Rfc2898DeriveBytes(password, salt, iterations))
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = rfc2898.GetBytes(32);
                aes.IV = rfc2898.GetBytes(16);

                using (var ms = new MemoryStream())
                using (var cryptoStream = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(encrypted, 0, encrypted.Length);
                    cryptoStream.FlushFinalBlock();
                    cryptoStream.Flush();

                    data = ms.ToArray();
                }
            }

            // Decompress
            using (var ms = new MemoryStream())
            using (var gzipStream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                gzipStream.CopyTo(ms);
                gzipStream.Close();

                data = ms.ToArray();
            }

            return Encoding.UTF8.GetString(data);
        }

        private static byte[] EncryptData(byte[] salt, string password, string contents)
        {
            var data = Encoding.UTF8.GetBytes(contents);

            // Compress
            using (var ms = new MemoryStream())
            using (var gzipStream = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzipStream.Write(data, 0, contents.Length);
                gzipStream.Close();

                data = ms.ToArray();
            }

            // Aes encrypt
            using (var rfc2898 = new Rfc2898DeriveBytes(password, salt, iterations))
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = rfc2898.GetBytes(32);
                aes.IV = rfc2898.GetBytes(16);

                using (var ms = new MemoryStream())
                using (var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    cryptoStream.Flush();

                    data = ms.ToArray();
                }
            }

            return data;
        }

        private static byte[] ComputeHash(byte[] data)
        {
            using (var hasher = SHA256.Create())
                return hasher.ComputeHash(data);
        }

        private static byte[] GenerateSalt()
        {
            using (var randomNumberGenerator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[32];
                randomNumberGenerator.GetBytes(randomNumber);
                return randomNumber;
            }
        }
    }
}