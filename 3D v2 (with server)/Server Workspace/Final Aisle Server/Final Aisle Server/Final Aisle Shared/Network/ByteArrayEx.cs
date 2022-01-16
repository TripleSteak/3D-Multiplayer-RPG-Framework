using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

/**
 * Class responsible for handling byte data manipulation (encrypption, compression, etc.)
 */
namespace Final_Aisle_Shared.Network
{
    public static class ByteArrayEx
    {
        public static IEnumerable<byte> Decompress(this IEnumerable<byte> data)
        {
            try
            {
                byte[] output = data.ToArray();
                using (var inputStream = new MemoryStream(output))
                using (var decompressor = new DeflateStream(inputStream, CompressionMode.Decompress))
                using (var decompressStream = new MemoryStream())
                {
                    decompressor.CopyTo(decompressStream);
                    decompressor.Close();
                    output = decompressStream.ToArray();
                }

                return output;
            } catch (InvalidDataException)
            { // previous bug from decompression resolved, throw still exists to prevent program abortion in case of error
                throw;
            }
        }

        public static IEnumerable<byte> Compress(this IEnumerable<byte> data)
        {
            byte[] output = null;
            using (var inputStream = new MemoryStream(data.ToArray()))
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(compressor);
                compressor.Close();
                output = compressStream.ToArray();
            }

            return output;
        }

        public static IEnumerable<byte> AESDecrypt(this IEnumerable<byte> data, SymmetricAlgorithm aes)
        {
            var output = data.Skip(sizeof(byte) * 16).ToArray();
            var iv = data.Take(sizeof(byte) * 16).ToArray();

            using (var inputStream = new MemoryStream(output))
            using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
            using (var decryptStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
            using (var outputStream = new MemoryStream())
            {
                decryptStream.CopyTo(outputStream);
                output = outputStream.ToArray();
            }

            return output;
        }

        public static IEnumerable<byte> AESEncrypt(this IEnumerable<byte> data, SymmetricAlgorithm aes)
        {
            byte[] output = data.ToArray();
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var encryptedStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(output, 0, output.Length);
                cryptoStream.Close();
                output = encryptedStream.ToArray();
                output = aes.IV.Concat(output).ToArray();
            }

            return output;
        }

        public static IEnumerable<byte> PrependLength(this IEnumerable<byte> data) => BitConverter.GetBytes(data.Count()).Concat(data);
    }
}
