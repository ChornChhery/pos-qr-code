using CatalogService.Contracts;
using CatalogService.Domain;
using Pos.Shared.Events;
using Pos.Shared.Money;

namespace CatalogService.Services;

public static class ProductMapper
{
    public static ProductDto ToDto(Product product, decimal usdToKhrRate, bool includeStock) =>
        new(
            product.Id,
            product.CategoryId,
            product.Name,
            product.Description,
            product.Price,
            CurrencyConverter.ToKhr(product.Price, usdToKhrRate),
            product.ImageUrl,
            // Managers see the raw switch so a PUT does not turn it off by accident; the public sees "can be ordered".
            includeStock ? product.IsAvailable : product.IsOrderable,
            includeStock ? product.StockCount : null,
            includeStock ? product.LowStockThreshold : null,
            product.Modifiers
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new ModifierDto(
                    m.Id,
                    m.GroupName,
                    m.IsRequired,
                    m.MinSelections,
                    m.MaxSelections,
                    m.DisplayOrder,
                    m.Options
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => new ModifierOptionDto(
                            o.Id, o.Name, o.PriceAdd, CurrencyConverter.ToKhr(o.PriceAdd, usdToKhrRate), o.DisplayOrder, o.IsAvailable))
                        .ToList()))
                .ToList());

    /// <summary>The message the Order service uses to price orders without calling Catalog.</summary>
    public static ProductSnapshot ToSnapshot(Product product) =>
        new(
            product.Id,
            product.CategoryId,
            product.Name,
            product.Price,
            product.IsAvailable,
            product.StockCount,
            product.Modifiers
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new ModifierSnapshot(
                    m.Id,
                    m.GroupName,
                    m.IsRequired,
                    m.MinSelections,
                    m.MaxSelections,
                    m.Options
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => new ModifierOptionSnapshot(o.Id, o.Name, o.PriceAdd, o.IsAvailable))
                        .ToList()))
                .ToList());
}
