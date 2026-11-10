using System.Security.Cryptography;

namespace CoffeeNChillGateway.Auth;

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool Verify(string password, string hash, string salt);
}

/// <summary>
/// Hashes passwords with PBKDF2 (HMAC-SHA256) and a random salt per user. Plain passwords are never stored.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        byte[] expected = Convert.FromBase64String(hash);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
            password, Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}