using CatalogService.Data;
using CatalogService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Pos.Shared.Events;

namespace CatalogService.Messaging;

/// <summary>
/// Deducts stock for a new order. Runs inside the MassTransit inbox/outbox transaction, so a redelivered message
/// is ignored and the resulting events are only published if the stock change is committed.
/// Concurrent changes to the same product raise a concurrency exception and the message is retried.
/// </summary>
public sealed class OrderCreatedConsumer(
    CatalogDbContext db,
    TimeProvider clock,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var order = context.Message;
        var wanted = order.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var ids = wanted.Keys.ToList();
        var products = await db.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(context.CancellationToken);

        foreach (var product in products)
        {
            if (product.StockCount is null)
            {
                continue;
            }

            var deduction = StockRules.Deduct(product.StockCount.Value, wanted[product.Id], product.LowStockThreshold);
            if (deduction.Oversold)
            {
                logger.LogWarning(
                    "Order {OrderId} needed more of product {ProductId} than was in stock; stock set to zero.",
                    order.OrderId, product.Id);
            }

            product.StockCount = deduction.NewStock;
            product.UpdatedAt = clock.GetUtcNow();

            await context.Publish(new StockChanged(product.Id, product.StockCount, product.IsAvailable));

            if (deduction.RaiseLowStockAlert)
            {
                await context.Publish(new LowStockAlert(
                    product.Id, product.Name, deduction.NewStock, product.LowStockThreshold));
            }
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
