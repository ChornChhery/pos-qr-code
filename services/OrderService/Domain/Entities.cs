namespace OrderService.Domain;

public enum OrderStatus
{
    Pending,
    Cooking,
    Ready,
    Completed,
    Cancelled
}

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    Cleaning
}

public sealed class DiningTable
{
    public Guid Id { get; init; }

    public int TableNumber { get; set; }

    public int Capacity { get; set; } = 4;

    public string? QrCodeUrl { get; set; }

    public TableStatus Status { get; set; } = TableStatus.Available;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class Order
{
    public Guid Id { get; init; }

    public Guid TableId { get; set; }

    public DiningTable? Table { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    /// <summary>Always zero: the shop charges no tax. Kept because the data model has the column.</summary>
    public decimal Tax { get; set; }

    public decimal Discount { get; set; }

    public decimal Total { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public List<OrderItem> Items { get; } = new();
}

public sealed class OrderItem
{
    public Guid Id { get; init; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Name at the time of the order, so later menu edits do not change old orders.</summary>
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>JSON array of the chosen options with their names and prices at the time of the order.</summary>
    public string? SelectedModifiers { get; set; }

    public string? Notes { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Local copy of a Catalog product, kept up to date from Catalog events.
/// It lets this service price and check an order without calling Catalog.
/// </summary>
public sealed class CatalogProduct
{
    public Guid Id { get; init; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }

    public int? StockCount { get; set; }

    /// <summary>JSON array of modifier groups and options (the Pos.Shared ModifierSnapshot shape).</summary>
    public string ModifiersJson { get; set; } = "[]";

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL xmin. Stops two checkouts from both taking the last item.</summary>
    public uint RowVersion { get; set; }
}
