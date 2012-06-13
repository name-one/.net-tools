using System.IO;
using System.Security.Cryptography;

namespace InoSoft.Tools.Net
{
    internal static class Extensions
    {
        public static void ReadAll(this Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int readCount = stream.Read(buffer, offset, count);
                offset += readCount;
                count -= readCount;
            }
        }

        public static byte[] ReadAll(this Stream stream, int count)
        {
            byte[] result = new byte[count];
            ReadAll(stream, result, 0, count);
            return result;
        }

        public static byte[] ReadAll(this Stream stream, int count, int blockSize)
        {
            if (blockSize > 0)
            {
                int remainder = count % blockSize;
                count = count - remainder + blockSize;
            }
            return ReadAll(stream, count);
        }

        public static byte[] Encrypt(this ICryptoTransform cryptoTransform, byte[] bytes)
        {
            using (MemoryStream resultMemoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(resultMemoryStream, cryptoTransform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                }
                return resultMemoryStream.ToArray();
            }
        }

        public static byte[] Decrypt(this ICryptoTransform cryptoTransform, byte[] bytes)
        {
            byte[] result = new byte[bytes.Length];
            using (CryptoStream cryptoStream = new CryptoStream(new MemoryStream(bytes), cryptoTransform, CryptoStreamMode.Read))
            {
                cryptoStream.Read(result, 0, result.Length);
            }
            return result;
        }

        public static void FinishBlock(this MemoryStream stream, int blockSize)
        {
            int remainder = (int)stream.Length % blockSize;
            if (remainder > 0)
            {
                remainder = blockSize - remainder;
                for (int i = 0; i < remainder; i++)
                {
                    stream.WriteByte(0);
                }
            }
        }
    }
}