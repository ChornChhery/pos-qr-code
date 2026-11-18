namespace CatalogService.Contracts;

public sealed record CategoryDto(Guid Id, string Name, string? Description, int DisplayOrder);

public sealed record ModifierOptionDto(
    Guid Id, string Name, decimal PriceAdd, long PriceAddKhr, int DisplayOrder, bool IsAvailable);

public sealed record ModifierDto(
    Guid Id,
    string GroupName,
    bool IsRequired,
    int MinSelections,
    int MaxSelections,
    int DisplayOrder,
    IReadOnlyList<ModifierOptionDto> Options);

/// <param name="Price">USD.</param>
/// <param name="PriceKhr">Riel, rounded to the nearest 100.</param>
/// <param name="StockCount">Only filled in for ADMIN and MANAGER callers. Null for the public.</param>
public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    long PriceKhr,
    string? ImageUrl,
    bool IsAvailable,
    int? StockCount,
    int? LowStockThreshold,
    IReadOnlyList<ModifierDto> Modifiers);
