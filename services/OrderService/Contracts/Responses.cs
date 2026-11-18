namespace OrderService.Contracts;

public sealed record TableDto(Guid Id, int TableNumber, int Capacity, string Status);

public sealed record SelectedModifierDto(Guid OptionId, string GroupName, string Name, decimal PriceAdd);

public sealed record CartLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    long UnitPriceKhr,
    IReadOnlyList<SelectedModifierDto> Modifiers,
    string? Notes,
    decimal LineTotal,
    long LineTotalKhr);

/// <summary>Server-calculated totals. USD is the stored amount; riel is rounded to the nearest 100 for display.</summary>
public sealed record CartDto(
    IReadOnlyList<CartLineDto> Lines,
    decimal Subtotal,
    decimal Tax,
    decimal Discount,
    decimal Total,
    long TotalKhr,
    decimal UsdToKhrRate);

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    long UnitPriceKhr,
    IReadOnlyList<SelectedModifierDto> Modifiers,
    string? Notes,
    decimal TotalPrice,
    long TotalPriceKhr);

public sealed record OrderDto(
    Guid Id,
    int TableNumber,
    string Status,
    string? Notes,
    decimal Subtotal,
    decimal Tax,
    decimal Discount,
    decimal Total,
    long TotalKhr,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
