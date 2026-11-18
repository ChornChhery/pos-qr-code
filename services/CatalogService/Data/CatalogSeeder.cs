using CatalogService.Domain;
using CatalogService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Pos.Shared.Events;

namespace CatalogService.Data;

/// <summary>Development sample menu, created only when there are no categories and Seed:SampleMenu is true.</summary>
public sealed class CatalogSeeder(CatalogDbContext db, IPublishEndpoint publish, TimeProvider clock, ILogger<CatalogSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct))
        {
            return;
        }

        var now = clock.GetUtcNow();
        var drinks = NewCategory("Drinks", 1, now);
        var mains = NewCategory("Mains", 2, now);
        var desserts = NewCategory("Desserts", 3, now);
        db.Categories.AddRange(drinks, mains, desserts);

        var products = new List<Product>
        {
            NewProduct(drinks, "Iced Latte", 2.50m, null, 10, now,
                NewModifier("Size", true, 1, 1, 1, now, ("Regular", 0m), ("Large", 0.50m)),
                NewModifier("Extras", false, 0, 2, 2, now, ("Extra shot", 0.50m), ("Oat milk", 0.60m))),
            NewProduct(drinks, "Lemon Tea", 1.50m, 50, 10, now),
            NewProduct(mains, "Beef Lok Lak", 5.50m, null, 10, now,
                NewModifier("Egg", false, 0, 1, 1, now, ("Fried egg", 0.50m))),
            NewProduct(mains, "Chicken Fried Rice", 4.00m, null, 10, now,
                NewModifier("Spice level", true, 1, 1, 1, now, ("Mild", 0m), ("Medium", 0m), ("Hot", 0m))),
            NewProduct(desserts, "Mango Sticky Rice", 3.00m, 20, 5, now),
            NewProduct(desserts, "Coconut Ice Cream", 2.00m, 10, 3, now)
        };

        db.Products.AddRange(products);

        foreach (var product in products)
        {
            await publish.Publish(new ProductCreated(ProductMapper.ToSnapshot(product)), ct);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded the sample menu ({Count} products).", products.Count);
    }

    private static Category NewCategory(string name, int order, DateTimeOffset now) =>
        new() { Id = Guid.CreateVersion7(), Name = name, DisplayOrder = order, IsActive = true, CreatedAt = now, UpdatedAt = now };

    private static Product NewProduct(
        Category category, string name, decimal price, int? stock, int threshold, DateTimeOffset now,
        params Modifier[] modifiers)
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            CategoryId = category.Id,
            Name = name,
            Price = price,
            IsAvailable = true,
            StockCount = stock,
            LowStockThreshold = threshold,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var modifier in modifiers)
        {
            product.Modifiers.Add(modifier);
        }

        // Link modifiers to the product id after it exists.
        foreach (var modifier in product.Modifiers)
        {
            modifier.ProductId = product.Id;
        }

        return product;
    }

    private static Modifier NewModifier(
        string group, bool required, int min, int max, int order, DateTimeOffset now,
        params (string Name, decimal PriceAdd)[] options)
    {
        var modifier = new Modifier
        {
            Id = Guid.CreateVersion7(),
            GroupName = group,
            IsRequired = required,
            MinSelections = min,
            MaxSelections = max,
            DisplayOrder = order,
            CreatedAt = now
        };

        var index = 0;
        foreach (var (name, priceAdd) in options)
        {
            modifier.Options.Add(new ModifierOption
            {
                Id = Guid.CreateVersion7(),
                ModifierId = modifier.Id,
                Name = name,
                PriceAdd = priceAdd,
                DisplayOrder = ++index,
                IsAvailable = true,
                CreatedAt = now
            });
        }

        return modifier;
    }
}
