using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

namespace OrderService.Data;

/// <summary>Creates tables 1..N when there are none and Seed:TableCount is above zero (Development).</summary>
public sealed class TableSeeder(OrderDbContext db, IConfiguration configuration, TimeProvider clock, ILogger<TableSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var count = configuration.GetValue("Seed:TableCount", 0);
        if (count <= 0 || await db.Tables.AnyAsync(ct))
        {
            return;
        }

        var now = clock.GetUtcNow();
        for (var number = 1; number <= count; number++)
        {
            db.Tables.Add(new DiningTable
            {
                Id = Guid.CreateVersion7(),
                TableNumber = number,
                Capacity = 4,
                Status = TableStatus.Available,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} tables.", count);
    }
}
