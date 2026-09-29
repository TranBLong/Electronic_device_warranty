using System.Text;

namespace Warranty.Services;

public static class PasswordHashing
{
    public static bool CanHash(string password) => Encoding.UTF8.GetByteCount(password) <= 72;

    public static string Hash(string password)
    {
        if (!CanHash(password))
        {
            throw new ArgumentException("Password cannot exceed 72 UTF-8 bytes.", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool Verify(string password, string passwordHash) =>
        CanHash(password) && BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
