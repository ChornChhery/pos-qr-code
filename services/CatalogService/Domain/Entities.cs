namespace CatalogService.Domain;

public sealed class Category
{
    public Guid Id { get; init; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<Product> Products { get; } = new();
}

public sealed class Product
{
    public Guid Id { get; init; }

    public Guid CategoryId { get; set; }

    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>USD, two decimals.</summary>
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Manual sold-out switch. False hides the product from ordering.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Null means stock is not tracked. Zero means out of stock.</summary>
    public int? StockCount { get; set; }

    public int LowStockThreshold { get; set; } = 10;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL xmin. Detects two people changing the same product at once.</summary>
    public uint RowVersion { get; set; }

    public List<Modifier> Modifiers { get; } = new();

    /// <summary>Can be ordered right now: switched on and not tracked-and-empty.</summary>
    public bool IsOrderable => IsAvailable && (StockCount is null || StockCount > 0);
}

public sealed class Modifier
{
    public Guid Id { get; init; }

    public Guid ProductId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public int MinSelections { get; set; }

    /// <summary>Zero means no upper limit.</summary>
    public int MaxSelections { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public List<ModifierOption> Options { get; } = new();
}

public sealed class ModifierOption
{
    public Guid Id { get; init; }

    public Guid ModifierId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>USD added to the product price when selected.</summary>
    public decimal PriceAdd { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTimeOffset CreatedAt { get; init; }
}
