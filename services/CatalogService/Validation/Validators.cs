using CatalogService.Contracts;
using FluentValidation;

namespace CatalogService.Validation;

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    private const decimal MaxMoney = 99_999_999.99m;

    public ProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).InclusiveBetween(0m, MaxMoney).PrecisionScale(10, 2, true)
            .WithMessage("Price must be between 0 and 99999999.99 with at most two decimals.");
        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .Must(BeHttpUrl).WithMessage("ImageUrl must be an http or https URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
        RuleFor(x => x.StockCount).GreaterThanOrEqualTo(0).When(x => x.StockCount.HasValue);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0).When(x => x.LowStockThreshold.HasValue);
        RuleFor(x => x.Modifiers).Must(m => m!.Count <= 20).When(x => x.Modifiers is not null)
            .WithMessage("A product can have at most 20 modifier groups.");
        RuleForEach(x => x.Modifiers).SetValidator(new ModifierInputValidator()).When(x => x.Modifiers is not null);
    }

    private static bool BeHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class ModifierInputValidator : AbstractValidator<ModifierInput>
{
    public ModifierInputValidator()
    {
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MinSelections).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxSelections).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.MaxSelections == 0 || x.MaxSelections >= x.MinSelections)
            .WithMessage("MaxSelections must be 0 (no limit) or at least MinSelections.");
        RuleFor(x => x).Must(x => !x.IsRequired || x.MinSelections >= 1)
            .WithMessage("A required modifier group needs MinSelections of at least 1.");
        RuleFor(x => x.Options).NotNull().Must(o => o is { Count: >= 1 and <= 30 })
            .WithMessage("A modifier group needs between 1 and 30 options.");
        RuleFor(x => x).Must(x => x.Options is null || x.Options.Count >= x.MinSelections)
            .WithMessage("A modifier group has fewer options than MinSelections.");
        RuleForEach(x => x.Options).SetValidator(new ModifierOptionInputValidator()).When(x => x.Options is not null);
    }
}

public sealed class ModifierOptionInputValidator : AbstractValidator<ModifierOptionInput>
{
    public ModifierOptionInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PriceAdd).InclusiveBetween(0m, 99_999_999.99m).PrecisionScale(10, 2, true)
            .WithMessage("PriceAdd must be between 0 and 99999999.99 with at most two decimals.");
    }
}
