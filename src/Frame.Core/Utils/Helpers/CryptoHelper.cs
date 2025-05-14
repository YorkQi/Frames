namespace Frame.Core
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public static class CryptoHelper
    {
        #region 哈希算法（不可逆，用于数据校验/密码存储）

        /// <summary>
        /// 计算字符串哈希值（自动选择算法）
        /// </summary>
        /// <param name="algorithm">可选：MD5, SHA1, SHA256, SHA512</param>
        public static string ComputeHash(string input, string algorithm = "SHA256")
        {
            using (var hashAlgorithm = HashAlgorithm.Create(algorithm))
            {
                if (hashAlgorithm == null)
                    throw new ArgumentException("不支持的算法类型", nameof(algorithm));

                byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(data).Replace("-", ""); // 16进制格式
            }
        }

        /// <summary>
        /// 计算文件哈希值（适合大文件）
        /// </summary>
        public static string ComputeFileHash(string filePath, string algorithm = "SHA256")
        {
            using (var stream = File.OpenRead(filePath))
            using (var hashAlgorithm = HashAlgorithm.Create(algorithm))
            {
                byte[] hashBytes = hashAlgorithm.ComputeHash(stream);
                return Convert.ToBase64String(hashBytes);
            }
        }
        #endregion

        #region AES 加密（对称加密，适合大数据）

        /// <summary>
        /// AES加密（自动生成密钥和IV）
        /// </summary>
        /// <returns>Base64加密结果</returns>
        public static string EncryptAES(string plainText, out byte[] key, out byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                key = aesAlg.Key;
                iv = aesAlg.IV;
                return EncryptAES(plainText, key, iv);
            }
        }

        /// <summary>
        /// AES加密（使用现有密钥和IV）
        /// </summary>
        public static string EncryptAES(string plainText, byte[] key, byte[] iv)
        {
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentNullException(nameof(plainText));
            if (key == null || key.Length == 0) throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length == 0) throw new ArgumentNullException(nameof(iv));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// AES解密
        /// </summary>
        public static string DecryptAES(string cipherText, byte[] key, byte[] iv)
        {
            if (string.IsNullOrEmpty(cipherText)) throw new ArgumentNullException(nameof(cipherText));
            if (key == null || key.Length == 0) throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length == 0) throw new ArgumentNullException(nameof(iv));

            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(buffer))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        #endregion

        #region RSA 加密（非对称加密，适合小数据或加密密钥）

        /// <summary>
        /// 生成RSA密钥对
        /// </summary>
        public static void GenerateRSAKeyPair(out string publicKey, out string privateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                publicKey = rsa.ToXmlString(false); // 公钥
                privateKey = rsa.ToXmlString(true); // 私钥
            }
        }

        /// <summary>
        /// RSA加密
        /// </summary>
        public static string EncryptRSA(string plainText, string publicKey)
        {
            byte[] data = Encoding.UTF8.GetBytes(plainText);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);
                byte[] encryptedData = rsa.Encrypt(data, true);
                return Convert.ToBase64String(encryptedData);
            }
        }

        /// <summary>
        /// RSA解密
        /// </summary>
        public static string DecryptRSA(string cipherText, string privateKey)
        {
            byte[] data = Convert.FromBase64String(cipherText);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                byte[] decryptedData = rsa.Decrypt(data, true);
                return Encoding.UTF8.GetString(decryptedData);
            }
        }
        #endregion
    }

}