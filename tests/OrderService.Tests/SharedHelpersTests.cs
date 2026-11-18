using FluentAssertions;
using OrderService.Domain;
using Pos.Shared.Http;
using Pos.Shared.Money;

namespace OrderService.Tests;

public class CurrencyConverterTests
{
    [Theory]
    [InlineData(2.50, 4000, 10000)]
    [InlineData(1.00, 4000, 4000)]
    [InlineData(0.02, 4000, 100)]
    [InlineData(0.01, 4000, 0)]
    [InlineData(2.50, 4100, 10300)] // 10250 riel rounds half up to the next 100
    [InlineData(3.51, 4000, 14000)] // 14040 rounds to 14000
    public void ToKhr_RoundsToTheNearestHundredRiel(double usd, int rate, long expected)
    {
        CurrencyConverter.ToKhr((decimal)usd, rate).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 4000)]
    [InlineData(-5, 4000)]
    [InlineData(5, 0)]
    public void ToKhr_ReturnsZeroForNonPositiveInput(double usd, int rate)
    {
        CurrencyConverter.ToKhr((decimal)usd, rate).Should().Be(0);
    }

    [Fact]
    public void RoundUsd_RoundsHalfAwayFromZero()
    {
        CurrencyConverter.RoundUsd(1.005m).Should().Be(1.01m);
        CurrencyConverter.RoundUsd(2.344m).Should().Be(2.34m);
    }
}

public class EnumWireTests
{
    [Theory]
    [InlineData("cooking", OrderStatus.Cooking)]
    [InlineData("PENDING", OrderStatus.Pending)]
    [InlineData(" Ready ", OrderStatus.Ready)]
    public void TryParseWire_AcceptsNamesInAnyCase(string text, OrderStatus expected)
    {
        EnumWire.TryParseWire<OrderStatus>(text, out var status).Should().BeTrue();
        status.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("SHIPPED")]
    public void TryParseWire_RejectsEverythingElse(string? text)
    {
        EnumWire.TryParseWire<OrderStatus>(text, out _).Should().BeFalse();
    }

    [Fact]
    public void ToWire_IsUpperCase()
    {
        OrderStatus.Cancelled.ToWire().Should().Be("CANCELLED");
        TableStatus.Available.ToWire().Should().Be("AVAILABLE");
    }
}
