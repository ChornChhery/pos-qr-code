using FluentAssertions;
using IdentityService.Domain;

namespace IdentityService.Tests;

public class UserRoleTests
{
    [Theory]
    [InlineData("CASHIER", UserRole.Cashier)]
    [InlineData("kitchen", UserRole.Kitchen)]
    [InlineData(" Manager ", UserRole.Manager)]
    [InlineData("ADMIN", UserRole.Admin)]
    public void TryParseWire_AcceptsKnownRoles(string input, UserRole expected)
    {
        UserRoleExtensions.TryParseWire(input, out var role).Should().BeTrue();
        role.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("GUEST")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("ADMIN,CASHIER")]
    public void TryParseWire_RejectsUnknownOrNumericRoles(string? input)
    {
        UserRoleExtensions.TryParseWire(input, out _).Should().BeFalse();
    }

    [Fact]
    public void ToWire_IsUpperCase()
    {
        UserRole.Admin.ToWire().Should().Be("ADMIN");
        UserRole.Cashier.ToWire().Should().Be("CASHIER");
    }
}
