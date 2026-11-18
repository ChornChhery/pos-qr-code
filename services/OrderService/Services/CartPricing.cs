using Pos.Shared.Events;
using Pos.Shared.Money;

namespace OrderService.Services;

public sealed record CartItemInput(Guid ProductId, int Quantity, IReadOnlyList<Guid>? ModifierOptionIds, string? Notes);

/// <summary>What pricing needs to know about a product. Built from the local copy of the Catalog product.</summary>
public sealed record PricingProduct(
    Guid Id,
    string Name,
    decimal Price,
    bool IsAvailable,
    int? StockCount,
    IReadOnlyList<ModifierSnapshot> Modifiers);

public sealed record PricedModifier(Guid OptionId, string GroupName, string Name, decimal PriceAdd);

public sealed record PricedLine(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    IReadOnlyList<PricedModifier> Modifiers,
    string? Notes,
    decimal LineTotal);

public sealed record PricingResult(
    bool Succeeded, IReadOnlyList<PricedLine> Lines, decimal Subtotal, IReadOnlyList<string> Errors);

/// <summary>
/// Server-side cart math. The client total is never trusted.
/// unit price = product price + chosen options; line total = unit price x quantity; no tax and no discount.
/// </summary>
public static class CartPricing
{
    public const int MaxQuantityPerLine = 99;
    public const decimal MaxMoney = 99_999_999.99m;

    public static PricingResult Price(
        IReadOnlyList<CartItemInput> items,
        IReadOnlyDictionary<Guid, PricingProduct> products)
    {
        var errors = new List<string>();
        var lines = new List<PricedLine>();

        if (items.Count == 0)
        {
            errors.Add("The cart is empty.");
            return Failed(errors);
        }

        CheckStock(items, products, errors);

        foreach (var item in items)
        {
            if (item.Quantity < 1 || item.Quantity > MaxQuantityPerLine)
            {
                errors.Add($"Quantity must be between 1 and {MaxQuantityPerLine}.");
                continue;
            }

            if (!products.TryGetValue(item.ProductId, out var product))
            {
                continue; // reported by CheckStock
            }

            var modifiers = PickModifiers(product, item.ModifierOptionIds ?? Array.Empty<Guid>(), errors);
            var unitPrice = CurrencyConverter.RoundUsd(product.Price + modifiers.Sum(m => m.PriceAdd));
            var lineTotal = CurrencyConverter.RoundUsd(unitPrice * item.Quantity);

            lines.Add(new PricedLine(
                product.Id,
                product.Name,
                item.Quantity,
                unitPrice,
                modifiers,
                string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim(),
                lineTotal));
        }

        var subtotal = CurrencyConverter.RoundUsd(lines.Sum(l => l.LineTotal));
        if (subtotal > MaxMoney)
        {
            errors.Add("The order total is too large.");
        }

        return errors.Count > 0 ? Failed(errors) : new PricingResult(true, lines, subtotal, Array.Empty<string>());
    }

    private static void CheckStock(
        IReadOnlyList<CartItemInput> items,
        IReadOnlyDictionary<Guid, PricingProduct> products,
        List<string> errors)
    {
        var quantityByProduct = items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => (long)Math.Max(i.Quantity, 0)));

        foreach (var (productId, quantity) in quantityByProduct)
        {
            if (!products.TryGetValue(productId, out var product))
            {
                errors.Add($"Product {productId} was not found.");
            }
            else if (!product.IsAvailable || product.StockCount is 0)
            {
                errors.Add($"{product.Name} is sold out.");
            }
            else if (product.StockCount is { } stock && quantity > stock)
            {
                errors.Add($"Only {stock} of {product.Name} left.");
            }
        }
    }

    private static List<PricedModifier> PickModifiers(PricingProduct product, IReadOnlyList<Guid> optionIds, List<string> errors)
    {
        var picked = new List<PricedModifier>();

        if (optionIds.Distinct().Count() != optionIds.Count)
        {
            errors.Add($"An option was chosen twice for {product.Name}.");
        }

        var chosen = optionIds.ToHashSet();
        var knownOptionIds = product.Modifiers.SelectMany(m => m.Options).Select(o => o.Id).ToHashSet();

        if (chosen.Any(id => !knownOptionIds.Contains(id)))
        {
            errors.Add($"An option is not valid for {product.Name}.");
        }

        foreach (var group in product.Modifiers)
        {
            var selected = group.Options.Where(o => chosen.Contains(o.Id)).ToList();

            foreach (var option in selected.Where(o => !o.IsAvailable))
            {
                errors.Add($"{option.Name} is not available.");
            }

            var minimum = group.IsRequired ? Math.Max(group.MinSelections, 1) : group.MinSelections;
            if (selected.Count < minimum)
            {
                errors.Add($"Choose at least {minimum} for {group.GroupName} on {product.Name}.");
            }

            if (group.MaxSelections > 0 && selected.Count > group.MaxSelections)
            {
                errors.Add($"Choose at most {group.MaxSelections} for {group.GroupName} on {product.Name}.");
            }

            picked.AddRange(selected.Select(o => new PricedModifier(o.Id, group.GroupName, o.Name, o.PriceAdd)));
        }

        return picked;
    }

    private static PricingResult Failed(List<string> errors) =>
        new(false, Array.Empty<PricedLine>(), 0m, errors.Distinct().ToList());
}
