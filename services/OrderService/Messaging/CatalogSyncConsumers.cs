using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Domain;
using Pos.Shared.Events;

namespace OrderService.Messaging;

/// <summary>
/// Keeps the local copy of the menu in step with Catalog. Each message means "this product now looks like this",
/// so repeated or re-published messages are harmless.
/// </summary>
public sealed class ProductSnapshotConsumer(OrderDbContext db, TimeProvider clock)
    : IConsumer<ProductCreated>, IConsumer<ProductUpdated>
{
    public Task Consume(ConsumeContext<ProductCreated> context) =>
        UpsertAsync(context.Message.Product, context.CancellationToken);

    public Task Consume(ConsumeContext<ProductUpdated> context) =>
        UpsertAsync(context.Message.Product, context.CancellationToken);

    private async Task UpsertAsync(ProductSnapshot snapshot, CancellationToken ct)
    {
        var existing = await db.CatalogProducts.FirstOrDefaultAsync(p => p.Id == snapshot.Id, ct);
        if (existing is null)
        {
            existing = new CatalogProduct { Id = snapshot.Id };
            db.CatalogProducts.Add(existing);
        }

        existing.Name = snapshot.Name;
        existing.Price = snapshot.Price;
        existing.IsAvailable = snapshot.IsAvailable;
        existing.StockCount = snapshot.StockCount;
        existing.ModifiersJson = JsonSerializer.Serialize(snapshot.Modifiers);
        existing.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
    }
}

public sealed class StockChangedConsumer(OrderDbContext db, TimeProvider clock) : IConsumer<StockChanged>
{
    public async Task Consume(ConsumeContext<StockChanged> context)
    {
        var message = context.Message;
        var product = await db.CatalogProducts.FirstOrDefaultAsync(p => p.Id == message.ProductId, context.CancellationToken);

        // Unknown product: the full snapshot has not arrived yet and will carry the stock itself.
        if (product is null)
        {
            return;
        }

        product.StockCount = message.StockCount;
        product.IsAvailable = message.IsAvailable;
        product.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
