using System.Security.Cryptography;
using System.Text;

namespace Nec.Web.Utils
{
    public static class EncryptionHelper
    {
        public static string Decrypt(string cipherText, string _key, string _iv)
        {
            var Key = Encoding.UTF8.GetBytes(_key);
            var IV = Encoding.UTF8.GetBytes(_iv);
            byte[] bytes = Convert.FromBase64String(cipherText);
            using (Aes aesAlg = Aes.Create())
            {

                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor();

                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            var red = srDecrypt.ReadToEnd();
                            return red;
                        }
                    }
                }
            }
        }
    }
}
