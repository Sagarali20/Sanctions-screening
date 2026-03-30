using System.Security.Cryptography;
using System.Text;

namespace Nec.Web.Utils
{
    public class CryptoJsAesDecryptor
    {
        public static string Decrypt(string encryptedBase64, string passphrase)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

            // Check for "Salted__"
            byte[] saltHeader = Encoding.ASCII.GetBytes("Salted__");

            if (!encryptedBytes.Take(8).SequenceEqual(saltHeader))
                throw new Exception("Invalid encrypted format");

            byte[] salt = encryptedBytes.Skip(8).Take(8).ToArray();
            byte[] cipherText = encryptedBytes.Skip(16).ToArray();

            using (var aes = Aes.Create())
            {
                var keyIv = DeriveKeyAndIv(passphrase, salt, aes.KeySize / 8, aes.BlockSize / 8);

                aes.Key = keyIv.Item1;
                aes.IV = keyIv.Item2;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static Tuple<byte[], byte[]> DeriveKeyAndIv(string password, byte[] salt, int keyLength, int ivLength)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var derivedBytes = new byte[0];
            byte[] block = null;

            using (var md5 = MD5.Create())
            {
                while (derivedBytes.Length < keyLength + ivLength)
                {
                    byte[] input = block == null
                        ? passwordBytes.Concat(salt).ToArray()
                        : block.Concat(passwordBytes).Concat(salt).ToArray();

                    block = md5.ComputeHash(input);
                    derivedBytes = derivedBytes.Concat(block).ToArray();
                }
            }

            return new Tuple<byte[], byte[]>(
                derivedBytes.Take(keyLength).ToArray(),
                derivedBytes.Skip(keyLength).Take(ivLength).ToArray()
            );
        }

    }
}
