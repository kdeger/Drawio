using System;
using System.Security.Cryptography;
using System.Text;

public class SaveEncryptor
{
    private const int IvSize = 16;

    private readonly byte[] _key;

    public SaveEncryptor(string key)
    {
        _key = Encoding.UTF8.GetBytes(key);

        if (_key.Length != 32)
            throw new ArgumentException("SaveEncryptor | Key must be 32 characters.");
    }

    public string Encrypt(string plainText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor())
            {
                var plain = Encoding.UTF8.GetBytes(plainText);
                var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
                var output = new byte[IvSize + cipher.Length];
                Buffer.BlockCopy(aes.IV, 0, output, 0, IvSize);
                Buffer.BlockCopy(cipher, 0, output, IvSize, cipher.Length);
                return Convert.ToBase64String(output);
            }
        }
    }

    public string Decrypt(string encryptedText)
    {
        try
        {
            var input = Convert.FromBase64String(encryptedText);
            var iv = new byte[IvSize];
            Buffer.BlockCopy(input, 0, iv, 0, IvSize);

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var plain = decryptor.TransformFinalBlock(input, IvSize, input.Length - IvSize);
                    return Encoding.UTF8.GetString(plain);
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
    }
}
