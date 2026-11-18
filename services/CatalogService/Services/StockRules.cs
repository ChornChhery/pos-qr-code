namespace CatalogService.Services;

public sealed record StockDeduction(int PreviousStock, int NewStock, bool Oversold, bool RaiseLowStockAlert);

public static class StockRules
{
    /// <summary>
    /// Removes <paramref name="quantity"/> from tracked stock. Stock never goes below zero.
    /// A low-stock alert is raised once, when stock crosses down to the threshold, or when it reaches zero.
    /// </summary>
    public static StockDeduction Deduct(int currentStock, int quantity, int lowStockThreshold)
    {
        var current = Math.Max(currentStock, 0);
        var removed = Math.Max(quantity, 0);
        var newStock = Math.Max(current - removed, 0);

        var crossedThreshold = lowStockThreshold > 0 && current > lowStockThreshold && newStock <= lowStockThreshold;
        var reachedZero = current > 0 && newStock == 0;

        return new StockDeduction(current, newStock, removed > current, crossedThreshold || reachedZero);
    }
}
