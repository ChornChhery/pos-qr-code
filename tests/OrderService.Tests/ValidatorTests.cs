using FluentAssertions;
using FluentValidation.TestHelper;
using OrderService.Contracts;
using OrderService.Validation;

namespace OrderService.Tests;

public class ValidatorTests
{
    private static CartItemRequest Item(int quantity = 1) => new(Guid.NewGuid(), quantity, null, null);

    [Fact]
    public void Checkout_AcceptsAValidOrder()
    {
        var request = new CheckoutRequest(12, "window seat", new List<CartItemRequest> { Item(2) });

        new CheckoutRequestValidator().TestValidate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(10_001)]
    public void Checkout_RejectsBadTableNumbers(int table)
    {
        new CheckoutRequestValidator()
            .TestValidate(new CheckoutRequest(table, null, new List<CartItemRequest> { Item() }))
            .ShouldHaveValidationErrorFor(x => x.TableNumber);
    }

    [Fact]
    public void Checkout_RejectsAnEmptyCart()
    {
        new CheckoutRequestValidator()
            .TestValidate(new CheckoutRequest(1, null, new List<CartItemRequest>()))
            .ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Checkout_RejectsMoreThanFiftyItems()
    {
        var items = Enumerable.Range(0, 51).Select(_ => Item()).ToList();

        new CheckoutRequestValidator()
            .TestValidate(new CheckoutRequest(1, null, items))
            .ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Checkout_RejectsLongNotes()
    {
        new CheckoutRequestValidator()
            .TestValidate(new CheckoutRequest(1, new string('x', 501), new List<CartItemRequest> { Item() }))
            .ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Cart_RejectsBadQuantities(int quantity)
    {
        new CartRequestValidator()
            .TestValidate(new CartRequest(new List<CartItemRequest> { Item(quantity) }))
            .IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("COOKING", true)]
    [InlineData("cancelled", true)]
    [InlineData("SHIPPED", false)]
    [InlineData("", false)]
    public void UpdateStatus_OnlyAcceptsKnownStatuses(string status, bool valid)
    {
        new UpdateStatusRequestValidator().TestValidate(new UpdateStatusRequest(status)).IsValid.Should().Be(valid);
    }
}
