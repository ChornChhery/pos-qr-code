using IdentityService.Domain;
using IdentityService.Options;
using IdentityService.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityService.Data;

/// <summary>Creates the first ADMIN account when the users table is empty.</summary>
public sealed class DbSeeder(
    IdentityDbContext db,
    IPasswordService passwords,
    IOptions<SeedOptions> options,
    TimeProvider clock,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning("No users exist and Seed:AdminPassword is not set. No admin account was created.");
            return;
        }

        if (seed.AdminPassword.Length < 10)
        {
            logger.LogWarning("Seed:AdminPassword is shorter than 10 characters. No admin account was created.");
            return;
        }

        var now = clock.GetUtcNow();
        db.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Username = seed.AdminUsername.Trim().ToLowerInvariant(),
            PasswordHash = passwords.Hash(seed.AdminPassword),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded the initial admin account.");
    }
}
