using CatalogService.Services;
using FluentAssertions;

namespace CatalogService.Tests;

public class StockRulesTests
{
    [Fact]
    public void Deduct_RemovesTheQuantity()
    {
        var result = StockRules.Deduct(currentStock: 10, quantity: 3, lowStockThreshold: 5);

        result.NewStock.Should().Be(7);
        result.PreviousStock.Should().Be(10);
        result.Oversold.Should().BeFalse();
        result.RaiseLowStockAlert.Should().BeFalse();
    }

    [Fact]
    public void Deduct_RaisesAnAlertWhenStockCrossesTheThreshold()
    {
        StockRules.Deduct(10, 6, 5).RaiseLowStockAlert.Should().BeTrue();
        StockRules.Deduct(6, 1, 5).RaiseLowStockAlert.Should().BeTrue();
    }

    [Fact]
    public void Deduct_DoesNotRepeatTheAlertWhileAlreadyBelowTheThreshold()
    {
        StockRules.Deduct(5, 1, 5).RaiseLowStockAlert.Should().BeFalse();
        StockRules.Deduct(4, 1, 5).RaiseLowStockAlert.Should().BeFalse();
    }

    [Fact]
    public void Deduct_RaisesAnAlertWhenStockReachesZero()
    {
        var result = StockRules.Deduct(1, 1, 5);

        result.NewStock.Should().Be(0);
        result.RaiseLowStockAlert.Should().BeTrue();
    }

    [Fact]
    public void Deduct_NeverGoesBelowZeroAndReportsOverselling()
    {
        var result = StockRules.Deduct(2, 5, 0);

        result.NewStock.Should().Be(0);
        result.Oversold.Should().BeTrue();
        result.RaiseLowStockAlert.Should().BeTrue();
    }

    [Fact]
    public void Deduct_FromZeroStaysAtZeroWithoutANewAlert()
    {
        var result = StockRules.Deduct(0, 1, 5);

        result.NewStock.Should().Be(0);
        result.Oversold.Should().BeTrue();
        result.RaiseLowStockAlert.Should().BeFalse();
    }

    [Fact]
    public void Deduct_ThresholdZeroMeansNoLowStockAlertUntilZero()
    {
        StockRules.Deduct(10, 3, 0).RaiseLowStockAlert.Should().BeFalse();
    }

    [Fact]
    public void Deduct_IgnoresNegativeQuantities()
    {
        StockRules.Deduct(10, -4, 5).NewStock.Should().Be(10);
    }
}
