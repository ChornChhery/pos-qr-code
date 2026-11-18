using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Options;
using IdentityService.Security;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace IdentityService.Tests;

internal static class TestSupport
{
    public const string SigningKey = "unit-test-signing-key-0123456789-abcdef";

    public static JwtOptions NewJwtOptions() => new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = SigningKey,
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    public static TokenService NewTokenService(JwtOptions? options = null) =>
        new(MsOptions.Create(options ?? NewJwtOptions()), TimeProvider.System);

    public static IdentityDbContext NewDb() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public static AuthService NewAuthService(IdentityDbContext db, IPasswordService? passwords = null) =>
        new(db, passwords ?? new PasswordService(), NewTokenService(), TimeProvider.System, NullLogger<AuthService>.Instance);

    public static RequestInfo Info { get; } = new("127.0.0.1", "unit-test");

    public static async Task<User> AddUserAsync(
        IdentityDbContext db,
        IPasswordService passwords,
        string username = "alice",
        string password = "correct-horse-battery",
        UserRole role = UserRole.Cashier,
        bool isActive = true)
    {
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            PasswordHash = passwords.Hash(password),
            Role = role,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
