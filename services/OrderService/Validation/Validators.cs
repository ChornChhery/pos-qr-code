using FluentValidation;
using OrderService.Contracts;
using OrderService.Domain;
using OrderService.Services;
using Pos.Shared.Http;

namespace OrderService.Validation;

public sealed class CartItemRequestValidator : AbstractValidator<CartItemRequest>
{
    public CartItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, CartPricing.MaxQuantityPerLine);
        RuleFor(x => x.ModifierOptionIds).Must(ids => ids!.Count <= 30)
            .When(x => x.ModifierOptionIds is not null)
            .WithMessage("Too many options on one item.");
        RuleFor(x => x.Notes).MaximumLength(200);
    }
}

public sealed class CartRequestValidator : AbstractValidator<CartRequest>
{
    public CartRequestValidator()
    {
        RuleFor(x => x.Items).NotNull().Must(items => items is { Count: >= 1 and <= 50 })
            .WithMessage("A cart needs between 1 and 50 items.");
        RuleForEach(x => x.Items).SetValidator(new CartItemRequestValidator()).When(x => x.Items is not null);
    }
}

public sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.TableNumber).InclusiveBetween(1, 10_000);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items).NotNull().Must(items => items is { Count: >= 1 and <= 50 })
            .WithMessage("An order needs between 1 and 50 items.");
        RuleForEach(x => x.Items).SetValidator(new CartItemRequestValidator()).When(x => x.Items is not null);
    }
}

public sealed class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => EnumWire.TryParseWire<OrderStatus>(status, out _))
            .WithMessage("Status must be one of PENDING, COOKING, READY, COMPLETED, CANCELLED.");
    }
}
