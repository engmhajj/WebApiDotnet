using System.Security.Cryptography;

namespace webapi.Security;

public static class SecretHasher
{
    private const int SALTSIZE = 16; // 128-bit salt
    private const int KEYSIZE = 32; // 256-bit hash
    private const int ITERATIONS = 100_000; // Adjust as needed for security/performance

    /// <summary>
    /// Generates a salt and hashes the secret using PBKDF2.
    /// </summary>
    public static (string Salt, string Hash) HashSecret(string secret)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SALTSIZE);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = HashWithPbkdf2(secret, saltBytes);
        return (salt, Convert.ToBase64String(hash));
    }

    /// <summary>
    /// Verifies the secret against the stored salt and hash.
    /// </summary>
    public static bool VerifySecret(string secret, string storedSalt, string storedHash)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        var expectedHash = Convert.FromBase64String(storedHash);
        var actualHash = HashWithPbkdf2(secret, saltBytes);

        // Compare in constant time to avoid timing attacks
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    /// <summary>
    /// Hashes the secret using PBKDF2.
    /// </summary>
    private static byte[] HashWithPbkdf2(string secret, byte[] saltBytes)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            secret,
            saltBytes,
            ITERATIONS,
            HashAlgorithmName.SHA256
        );
        return pbkdf2.GetBytes(KEYSIZE);
    }
}
