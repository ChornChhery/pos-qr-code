using CatalogService.Contracts;
using CatalogService.Validation;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CatalogService.Tests;

public class ValidatorTests
{
    private static ProductRequest Valid(
        decimal price = 2.50m, string? imageUrl = null, List<ModifierInput>? modifiers = null, int? stock = null) =>
        new(Guid.NewGuid(), "Iced Latte", "Cold coffee", price, imageUrl, true, stock, 10, modifiers);

    private static ModifierInput Group(
        bool required = false, int min = 0, int max = 0, params ModifierOptionInput[] options) =>
        new("Size", required, min, max, 1, options.ToList());

    private static ModifierOptionInput Option(string name = "Large", decimal priceAdd = 0.5m) => new(name, priceAdd, 1, null);

    [Fact]
    public void Product_AcceptsAValidRequest()
    {
        var request = Valid(modifiers: new List<ModifierInput> { Group(true, 1, 1, Option("Regular", 0m), Option()) });

        new ProductRequestValidator().TestValidate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(2.505)]
    [InlineData(100000000)]
    public void Product_RejectsBadPrices(double price)
    {
        new ProductRequestValidator().TestValidate(Valid(price: (decimal)price)).ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/a.png")]
    [InlineData("not a url")]
    public void Product_RejectsImageUrlsThatAreNotHttp(string url)
    {
        new ProductRequestValidator().TestValidate(Valid(imageUrl: url)).ShouldHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void Product_AcceptsAnHttpsImageUrl()
    {
        new ProductRequestValidator().TestValidate(Valid(imageUrl: "https://cdn.example.com/latte.jpg")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Product_RejectsNegativeStock()
    {
        new ProductRequestValidator().TestValidate(Valid(stock: -1)).ShouldHaveValidationErrorFor(x => x.StockCount);
    }

    [Fact]
    public void Product_RejectsABlankName()
    {
        var request = Valid() with { Name = "  " };

        new ProductRequestValidator().TestValidate(request).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Modifier_RequiredGroupNeedsAtLeastOneSelection()
    {
        var result = new ModifierInputValidator().TestValidate(Group(required: true, min: 0, max: 1, Option()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Modifier_MaxBelowMinIsRejected()
    {
        new ModifierInputValidator().TestValidate(Group(false, 2, 1, Option(), Option("B"))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Modifier_MaxZeroMeansNoLimit()
    {
        new ModifierInputValidator().TestValidate(Group(false, 0, 0, Option())).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Modifier_NeedsAtLeastOneOption()
    {
        new ModifierInputValidator().TestValidate(Group()).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Modifier_NeedsAtLeastAsManyOptionsAsTheMinimum()
    {
        new ModifierInputValidator().TestValidate(Group(true, 2, 2, Option())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Option_RejectsNegativePriceAdd()
    {
        new ModifierOptionInputValidator().TestValidate(Option(priceAdd: -1m)).ShouldHaveValidationErrorFor(x => x.PriceAdd);
    }
}
