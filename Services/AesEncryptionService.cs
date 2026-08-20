using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace MedLinkPortal.Services
{
    public class AesEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private const int KeySize = 32; // 256 bits
        private const int IvSize = 16;  // 128 bits
        private const int TagSize = 32; // HMAC-SHA256 size

        public AesEncryptionService(IConfiguration configuration)
        {
            var keyString = configuration["EncryptionSettings:Key"] ?? "Default-MedLink-Fallback-Key-32-Chars";
            using (var deriveBytes = new Rfc2898DeriveBytes(keyString, Encoding.UTF8.GetBytes("MedLinkSalt"), 10000))
            {
                _key = deriveBytes.GetBytes(KeySize);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length); // Prepend IV

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    var encryptedContent = ms.ToArray();

                    // Add integrity check (HMAC-SHA256)
                    using (var hmac = new HMACSHA256(_key))
                    {
                        var hash = hmac.ComputeHash(encryptedContent);
                        var finalPayload = new byte[hash.Length + encryptedContent.Length];
                        Buffer.BlockCopy(hash, 0, finalPayload, 0, hash.Length);
                        Buffer.BlockCopy(encryptedContent, 0, finalPayload, hash.Length, encryptedContent.Length);
                        
                        return Convert.ToBase64String(finalPayload);
                    }
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var fullPayload = Convert.FromBase64String(cipherText);
                if (fullPayload.Length < TagSize + IvSize) return cipherText;

                var hash = new byte[TagSize];
                var encryptedContent = new byte[fullPayload.Length - TagSize];
                Buffer.BlockCopy(fullPayload, 0, hash, 0, TagSize);
                Buffer.BlockCopy(fullPayload, TagSize, encryptedContent, 0, encryptedContent.Length);

                // Verify integrity
                using (var hmac = new HMACSHA256(_key))
                {
                    var computedHash = hmac.ComputeHash(encryptedContent);
                    for (int i = 0; i < TagSize; i++)
                    {
                        if (hash[i] != computedHash[i]) throw new CryptographicException("Encryption integrity check failed.");
                    }
                }

                var iv = new byte[IvSize];
                Buffer.BlockCopy(encryptedContent, 0, iv, 0, IvSize);

                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(encryptedContent, IvSize, encryptedContent.Length - IvSize))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // If decryption fails, it might be unencrypted data (e.g. legacy records)
                return cipherText;
            }
        }
    }
}
