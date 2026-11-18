using CatalogService.Domain;
using CatalogService.Services;
using FluentAssertions;

namespace CatalogService.Tests;

public class ProductMapperTests
{
    private static Product NewProduct(bool available = true, int? stock = null)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Name = "Iced Latte",
            Price = 2.50m,
            IsAvailable = available,
            StockCount = stock,
            LowStockThreshold = 10
        };

        var size = new Modifier { Id = Guid.NewGuid(), ProductId = product.Id, GroupName = "Size", IsRequired = true, MinSelections = 1, MaxSelections = 1, DisplayOrder = 1 };
        size.Options.Add(new ModifierOption { Id = Guid.NewGuid(), ModifierId = size.Id, Name = "Large", PriceAdd = 0.50m, DisplayOrder = 2, IsAvailable = true });
        size.Options.Add(new ModifierOption { Id = Guid.NewGuid(), ModifierId = size.Id, Name = "Regular", PriceAdd = 0m, DisplayOrder = 1, IsAvailable = true });
        product.Modifiers.Add(size);
        return product;
    }

    [Fact]
    public void ToDto_ShowsRielNextToDollars()
    {
        var dto = ProductMapper.ToDto(NewProduct(), 4000m, includeStock: false);

        dto.Price.Should().Be(2.50m);
        dto.PriceKhr.Should().Be(10000);
        dto.Modifiers.Single().Options.Select(o => o.PriceAddKhr).Should().Equal(0, 2000);
    }

    [Fact]
    public void ToDto_OrdersOptionsByDisplayOrder()
    {
        var dto = ProductMapper.ToDto(NewProduct(), 4000m, includeStock: false);

        dto.Modifiers.Single().Options.Select(o => o.Name).Should().Equal("Regular", "Large");
    }

    [Fact]
    public void ToDto_HidesStockFromThePublic()
    {
        var dto = ProductMapper.ToDto(NewProduct(stock: 7), 4000m, includeStock: false);

        dto.StockCount.Should().BeNull();
        dto.LowStockThreshold.Should().BeNull();
    }

    [Fact]
    public void ToDto_ShowsStockToManagers()
    {
        var dto = ProductMapper.ToDto(NewProduct(stock: 7), 4000m, includeStock: true);

        dto.StockCount.Should().Be(7);
        dto.LowStockThreshold.Should().Be(10);
    }

    [Fact]
    public void ToDto_PublicSeesOutOfStockAsNotAvailable()
    {
        ProductMapper.ToDto(NewProduct(stock: 0), 4000m, includeStock: false).IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void ToDto_ManagersSeeTheRawSwitchSoASaveDoesNotTurnItOffByAccident()
    {
        ProductMapper.ToDto(NewProduct(available: true, stock: 0), 4000m, includeStock: true).IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void ToSnapshot_CarriesPriceStockAndModifiers()
    {
        var product = NewProduct(stock: 3);

        var snapshot = ProductMapper.ToSnapshot(product);

        snapshot.Id.Should().Be(product.Id);
        snapshot.Price.Should().Be(2.50m);
        snapshot.StockCount.Should().Be(3);
        snapshot.Modifiers.Single().Options.Should().HaveCount(2);
        snapshot.Modifiers.Single().Options.First().Name.Should().Be("Regular");
    }

    [Fact]
    public void IsOrderable_IsFalseWhenSwitchedOffOrTrackedAndEmpty()
    {
        NewProduct(available: false).IsOrderable.Should().BeFalse();
        NewProduct(stock: 0).IsOrderable.Should().BeFalse();
        NewProduct(stock: 1).IsOrderable.Should().BeTrue();
        NewProduct(stock: null).IsOrderable.Should().BeTrue();
    }
}
