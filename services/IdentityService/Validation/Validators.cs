using FluentValidation;
using IdentityService.Contracts;
using IdentityService.Domain;

namespace IdentityService.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may contain letters, digits, dot, underscore, and hyphen only.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(128);

        RuleFor(x => x.Role)
            .Must(role => UserRoleExtensions.TryParseWire(role, out _))
            .WithMessage("Role must be one of CASHIER, KITCHEN, MANAGER, ADMIN.");
    }
}

public sealed class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .Must(role => UserRoleExtensions.TryParseWire(role, out _))
            .WithMessage("Role must be one of CASHIER, KITCHEN, MANAGER, ADMIN.");
    }
}
