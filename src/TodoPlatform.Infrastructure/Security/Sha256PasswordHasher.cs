using System.Security.Cryptography;
using System.Text;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Security;

public sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public bool Verify(string password, string hash) =>
        Hash(password) == hash;
}
