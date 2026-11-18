using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityService.Data;

/// <summary>
/// Used only by "dotnet ef" so that creating a migration does not run Program.cs
/// (which would try to connect to the database). No connection is opened at design time.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=identity_db")
            .Options;

        return new IdentityDbContext(options);
    }
}
