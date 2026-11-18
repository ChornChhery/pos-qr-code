namespace Pos.Shared.Events;

// Message contracts shared by Catalog, Order, and Notification.
// Rules: only add optional members; never rename or remove one without a new message type.

public sealed record ModifierOptionSnapshot(Guid Id, string Name, decimal PriceAdd, bool IsAvailable);

/// <param name="MaxSelections">0 means no upper limit.</param>
public sealed record ModifierSnapshot(
    Guid Id,
    string GroupName,
    bool IsRequired,
    int MinSelections,
    int MaxSelections,
    IReadOnlyList<ModifierOptionSnapshot> Options);

/// <summary>Everything the Order service needs to price an order without calling Catalog.</summary>
/// <param name="StockCount">Null means stock is not tracked for this product.</param>
public sealed record ProductSnapshot(
    Guid Id,
    Guid CategoryId,
    string Name,
    decimal Price,
    bool IsAvailable,
    int? StockCount,
    IReadOnlyList<ModifierSnapshot> Modifiers);

public sealed record ProductCreated(ProductSnapshot Product);

public sealed record ProductUpdated(ProductSnapshot Product);

/// <summary>Absolute stock after a change. Order uses it to refuse sold-out items.</summary>
public sealed record StockChanged(Guid ProductId, int? StockCount, bool IsAvailable);

public sealed record LowStockAlert(Guid ProductId, string ProductName, int StockCount, int Threshold);

public sealed record OrderLine(Guid ProductId, string ProductName, int Quantity, string? Notes, IReadOnlyList<string> Modifiers);

public sealed record OrderCreated(
    Guid OrderId,
    int TableNumber,
    decimal Total,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLine> Items);

public sealed record OrderStatusChanged(
    Guid OrderId,
    int TableNumber,
    string PreviousStatus,
    string Status,
    DateTimeOffset ChangedAt);
