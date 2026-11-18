using CatalogService.Contracts;
using CatalogService.Data;
using CatalogService.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pos.Shared.Events;
using Pos.Shared.Http;
using Pos.Shared.Money;

namespace CatalogService.Services;

/// <summary>Product writes. Every change publishes an event through the transactional outbox.</summary>
public sealed class ProductService(
    CatalogDbContext db,
    IPublishEndpoint publish,
    TimeProvider clock,
    IOptions<CurrencyOptions> currency)
{
    private decimal Rate => currency.Value.UsdToKhrRate;

    public async Task<ServiceResult<ProductDto>> CreateAsync(ProductRequest request, CancellationToken ct = default)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive, ct))
        {
            return ServiceResult<ProductDto>.Fail(ErrorKind.Validation, "Category not found.");
        }

        var now = clock.GetUtcNow();
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            CategoryId = request.CategoryId,
            CreatedAt = now,
            UpdatedAt = now
        };

        Apply(product, request, now);
        db.Products.Add(product);
        await publish.Publish(new ProductCreated(ProductMapper.ToSnapshot(product)), ct);
        await db.SaveChangesAsync(ct);

        return ServiceResult<ProductDto>.Ok(ProductMapper.ToDto(product, Rate, includeStock: true));
    }

    public async Task<ServiceResult<ProductDto>> UpdateAsync(Guid id, ProductRequest request, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Modifiers).ThenInclude(m => m.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail(ErrorKind.NotFound, "Product not found.");
        }

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive, ct))
        {
            return ServiceResult<ProductDto>.Fail(ErrorKind.Validation, "Category not found.");
        }

        var now = clock.GetUtcNow();
        product.CategoryId = request.CategoryId;

        // Modifiers are replaced as a whole. Carts that still hold old option ids are rejected at the next price check.
        db.Modifiers.RemoveRange(product.Modifiers);
        product.Modifiers.Clear();

        Apply(product, request, now);
        await publish.Publish(new ProductUpdated(ProductMapper.ToSnapshot(product)), ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<ProductDto>.Fail(
                ErrorKind.Conflict, "The product was changed by someone else. Reload it and try again.");
        }

        return ServiceResult<ProductDto>.Ok(ProductMapper.ToDto(product, Rate, includeStock: true));
    }

    public async Task<ServiceResult<ProductDto>> SetSoldOutAsync(Guid id, bool isSoldOut, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Modifiers).ThenInclude(m => m.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail(ErrorKind.NotFound, "Product not found.");
        }

        product.IsAvailable = !isSoldOut;
        product.UpdatedAt = clock.GetUtcNow();
        await publish.Publish(new ProductUpdated(ProductMapper.ToSnapshot(product)), ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<ProductDto>.Fail(
                ErrorKind.Conflict, "The product was changed by someone else. Reload it and try again.");
        }

        return ServiceResult<ProductDto>.Ok(ProductMapper.ToDto(product, Rate, includeStock: true));
    }

    private static void Apply(Product product, ProductRequest request, DateTimeOffset now)
    {
        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = CurrencyConverter.RoundUsd(request.Price);
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.IsAvailable = request.IsAvailable ?? product.IsAvailable;
        product.StockCount = request.StockCount;
        product.LowStockThreshold = request.LowStockThreshold ?? product.LowStockThreshold;
        product.UpdatedAt = now;

        foreach (var input in request.Modifiers ?? new List<ModifierInput>())
        {
            var modifier = new Modifier
            {
                Id = Guid.CreateVersion7(),
                ProductId = product.Id,
                GroupName = input.GroupName.Trim(),
                IsRequired = input.IsRequired,
                MinSelections = input.MinSelections,
                MaxSelections = input.MaxSelections,
                DisplayOrder = input.DisplayOrder,
                CreatedAt = now
            };

            foreach (var option in input.Options)
            {
                modifier.Options.Add(new ModifierOption
                {
                    Id = Guid.CreateVersion7(),
                    ModifierId = modifier.Id,
                    Name = option.Name.Trim(),
                    PriceAdd = CurrencyConverter.RoundUsd(option.PriceAdd),
                    DisplayOrder = option.DisplayOrder,
                    IsAvailable = option.IsAvailable ?? true,
                    CreatedAt = now
                });
            }

            product.Modifiers.Add(modifier);
        }
    }
}
