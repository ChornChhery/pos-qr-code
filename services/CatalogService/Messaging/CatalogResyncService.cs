using CatalogService.Data;
using CatalogService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Pos.Shared.Events;

namespace CatalogService.Messaging;

/// <summary>
/// The Order service keeps its own copy of the menu, fed by events. Events published before the Order service
/// has created its queue are lost, so Catalog re-publishes every product shortly after start and then periodically.
/// Order treats each message as "set this product to this state", so repeats are harmless.
/// </summary>
public sealed class CatalogResyncService(IServiceScopeFactory scopes, ILogger<CatalogResyncService> logger) : BackgroundService
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await ResyncAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task ResyncAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var products = await db.Products
                .AsNoTracking()
                .Include(p => p.Modifiers).ThenInclude(m => m.Options)
                .AsSplitQuery()
                .ToListAsync(ct);

            foreach (var product in products)
            {
                await publish.Publish(new ProductUpdated(ProductMapper.ToSnapshot(product)), ct);
            }

            // Saving flushes the queued messages into the outbox table in one transaction.
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Catalog resync queued {Count} product snapshots.", products.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Catalog resync failed; it will be retried at the next interval.");
        }
    }
}
