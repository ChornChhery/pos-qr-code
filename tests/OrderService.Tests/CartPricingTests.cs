using FluentAssertions;
using OrderService.Services;
using Pos.Shared.Events;

namespace OrderService.Tests;

public class CartPricingTests
{
    private static readonly Guid Latte = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Tea = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid SizeRegular = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid SizeLarge = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid ExtraShot = Guid.Parse("00000000-0000-0000-0000-0000000000b3");
    private static readonly Guid OatMilk = Guid.Parse("00000000-0000-0000-0000-0000000000b4");
    private static readonly Guid Unavailable = Guid.Parse("00000000-0000-0000-0000-0000000000b5");

    private static PricingProduct LatteProduct(bool available = true, int? stock = null) => new(
        Latte, "Iced Latte", 2.50m, available, stock,
        new List<ModifierSnapshot>
        {
            new(Guid.NewGuid(), "Size", true, 1, 1, new List<ModifierOptionSnapshot>
            {
                new(SizeRegular, "Regular", 0m, true),
                new(SizeLarge, "Large", 0.50m, true)
            }),
            new(Guid.NewGuid(), "Extras", false, 0, 2, new List<ModifierOptionSnapshot>
            {
                new(ExtraShot, "Extra shot", 0.50m, true),
                new(OatMilk, "Oat milk", 0.60m, true),
                new(Unavailable, "Sold out syrup", 0.30m, false)
            })
        });

    private static PricingProduct TeaProduct(int? stock = null) =>
        new(Tea, "Lemon Tea", 1.50m, true, stock, new List<ModifierSnapshot>());

    private static Dictionary<Guid, PricingProduct> Menu(params PricingProduct[] products) =>
        products.ToDictionary(p => p.Id);

    private static CartItemInput Item(Guid product, int quantity, params Guid[] options) =>
        new(product, quantity, options, null);

    [Fact]
    public void Price_PlainProduct_IsUnitPriceTimesQuantity()
    {
        var result = CartPricing.Price(new[] { Item(Tea, 2) }, Menu(TeaProduct()));

        result.Succeeded.Should().BeTrue();
        result.Lines.Single().UnitPrice.Should().Be(1.50m);
        result.Lines.Single().LineTotal.Should().Be(3.00m);
        result.Subtotal.Should().Be(3.00m);
    }

    [Fact]
    public void Price_AddsModifierPricesToTheUnitPrice()
    {
        // 2.50 + Large 0.50 + Extra shot 0.50 = 3.50 per drink, three drinks = 10.50
        var result = CartPricing.Price(new[] { Item(Latte, 3, SizeLarge, ExtraShot) }, Menu(LatteProduct()));

        result.Succeeded.Should().BeTrue();
        var line = result.Lines.Single();
        line.UnitPrice.Should().Be(3.50m);
        line.LineTotal.Should().Be(10.50m);
        line.Modifiers.Select(m => m.Name).Should().BeEquivalentTo("Large", "Extra shot");
    }

    [Fact]
    public void Price_SumsSeveralLines()
    {
        var result = CartPricing.Price(
            new[] { Item(Latte, 1, SizeRegular), Item(Tea, 4) },
            Menu(LatteProduct(), TeaProduct()));

        result.Subtotal.Should().Be(2.50m + 6.00m);
    }

    [Fact]
    public void Price_EmptyCart_Fails()
    {
        var result = CartPricing.Price(Array.Empty<CartItemInput>(), Menu(TeaProduct()));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("empty");
    }

    [Fact]
    public void Price_UnknownProduct_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Guid.NewGuid(), 1) }, Menu(TeaProduct()));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public void Price_SoldOutProduct_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Latte, 1, SizeRegular) }, Menu(LatteProduct(available: false)));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("sold out"));
    }

    [Fact]
    public void Price_ZeroStock_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Tea, 1) }, Menu(TeaProduct(stock: 0)));

        result.Errors.Should().Contain(e => e.Contains("sold out"));
    }

    [Fact]
    public void Price_MoreThanStock_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Tea, 4) }, Menu(TeaProduct(stock: 3)));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Only 3 of Lemon Tea left.");
    }

    [Fact]
    public void Price_StockIsCheckedAcrossLinesOfTheSameProduct()
    {
        var result = CartPricing.Price(new[] { Item(Tea, 2), Item(Tea, 2) }, Menu(TeaProduct(stock: 3)));

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Price_ExactlyTheStock_Succeeds()
    {
        CartPricing.Price(new[] { Item(Tea, 3) }, Menu(TeaProduct(stock: 3))).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Price_RequiredGroupMissing_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Latte, 1) }, Menu(LatteProduct()));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Choose at least 1 for Size"));
    }

    [Fact]
    public void Price_TooManyInOneGroup_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Latte, 1, SizeRegular, SizeLarge) }, Menu(LatteProduct()));

        result.Errors.Should().Contain(e => e.Contains("Choose at most 1 for Size"));
    }

    [Fact]
    public void Price_UnavailableOption_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Latte, 1, SizeRegular, Unavailable) }, Menu(LatteProduct()));

        result.Errors.Should().Contain(e => e.Contains("not available"));
    }

    [Fact]
    public void Price_OptionFromAnotherProduct_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Tea, 1, SizeLarge) }, Menu(TeaProduct()));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not valid"));
    }

    [Fact]
    public void Price_SameOptionTwice_Fails()
    {
        var result = CartPricing.Price(new[] { Item(Latte, 1, SizeRegular, ExtraShot, ExtraShot) }, Menu(LatteProduct()));

        result.Errors.Should().Contain(e => e.Contains("twice"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Price_QuantityOutOfRange_Fails(int quantity)
    {
        CartPricing.Price(new[] { Item(Tea, quantity) }, Menu(TeaProduct())).Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Price_HasNoTax_TotalEqualsSubtotal()
    {
        var result = CartPricing.Price(new[] { Item(Tea, 7) }, Menu(TeaProduct()));

        result.Subtotal.Should().Be(10.50m);
    }

    [Fact]
    public void Price_TotalAboveTheMoneyLimit_Fails()
    {
        var expensive = new PricingProduct(Tea, "Gold tea", 99_999_999.00m, true, null, new List<ModifierSnapshot>());

        var result = CartPricing.Price(new[] { Item(Tea, 2) }, Menu(expensive));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("too large"));
    }

    [Fact]
    public void Price_TrimsNotesAndDropsBlankNotes()
    {
        var withNote = new CartItemInput(Tea, 1, null, "  no ice  ");
        var blank = new CartItemInput(Tea, 1, null, "   ");

        var result = CartPricing.Price(new[] { withNote, blank }, Menu(TeaProduct()));

        result.Lines[0].Notes.Should().Be("no ice");
        result.Lines[1].Notes.Should().BeNull();
    }
}
