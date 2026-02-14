using System;
using System.Security.Cryptography;

public static class PasswordHelper
{
    private const int SaltSize = 16;      // 128-bit salt
    private const int HashSize = 32;      // 256-bit hash
    private const int Iterations = 100000; // Number of PBKDF2 iterations

    // 🔐 Hash password
    public static string HashPassword(string password)
    {
        byte[] salt = new byte[SaltSize];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt);
        }

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(HashSize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }

    // 🔑 Verify password
    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3)
            return false;

        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] stored = Convert.FromBase64String(parts[2]);

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            byte[] computed = pbkdf2.GetBytes(stored.Length);
            return AreHashesEqual(stored, computed);
        }
    }

    // Constant-time comparison for .NET Framework
    private static bool AreHashesEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}
