namespace CatalogService.Contracts;

public sealed record ModifierOptionInput(string Name, decimal PriceAdd, int DisplayOrder, bool? IsAvailable);

/// <param name="MaxSelections">0 means no upper limit.</param>
public sealed record ModifierInput(
    string GroupName,
    bool IsRequired,
    int MinSelections,
    int MaxSelections,
    int DisplayOrder,
    List<ModifierOptionInput> Options);

/// <summary>Used for both create and full update. On update the modifiers are replaced as a whole.</summary>
/// <param name="StockCount">Null means stock is not tracked.</param>
public sealed record ProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool? IsAvailable,
    int? StockCount,
    int? LowStockThreshold,
    List<ModifierInput>? Modifiers);

public sealed record SoldOutRequest(bool IsSoldOut);
