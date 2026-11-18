using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Security;

public interface IPasswordService
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);

    /// <summary>
    /// Performs a hash verification against a dummy hash so that a login for an unknown user
    /// takes about as long as one for a known user.
    /// </summary>
    void BurnVerify(string password);
}

/// <summary>PBKDF2 password hashing through the ASP.NET Core PasswordHasher.</summary>
public sealed class PasswordService : IPasswordService
{
    private static readonly User HasherSubject = new() { Username = string.Empty, PasswordHash = string.Empty };

    private readonly PasswordHasher<User> _hasher = new();
    private readonly string _dummyHash;

    public PasswordService()
    {
        _dummyHash = _hasher.HashPassword(HasherSubject, Guid.NewGuid().ToString("N"));
    }

    public string Hash(string password) => _hasher.HashPassword(HasherSubject, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(HasherSubject, passwordHash, password) != PasswordVerificationResult.Failed;

    public void BurnVerify(string password) =>
        _ = _hasher.VerifyHashedPassword(HasherSubject, _dummyHash, password);
}
