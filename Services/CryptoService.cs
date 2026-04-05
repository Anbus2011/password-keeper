using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pass.Services;

public static class CryptoService
{
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000;
    private const int HeaderSize = SaltSize + IvSize; // 32 bytes

    public static byte[] Encrypt(string plaintext, string masterPassword)
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        var key = DeriveKey(masterPassword, salt);
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream();
            ms.Write(salt, 0, SaltSize);
            ms.Write(iv, 0, IvSize);

            using (var encryptor = aes.CreateEncryptor())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            }

            return ms.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public static string Decrypt(byte[] fileBytes, string masterPassword)
    {
        if (fileBytes.Length < HeaderSize)
            throw new CryptographicException("File is too small to be a valid vault.");

        var salt = fileBytes.AsSpan(0, SaltSize).ToArray();
        var iv = fileBytes.AsSpan(SaltSize, IvSize).ToArray();
        var ciphertext = fileBytes.AsSpan(HeaderSize).ToArray();

        var key = DeriveKey(masterPassword, salt);
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream(ciphertext);
            using var decryptor = aes.CreateDecryptor();
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, salt, Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }
}
