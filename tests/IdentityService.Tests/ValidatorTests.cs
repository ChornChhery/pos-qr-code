using FluentAssertions;
using FluentValidation.TestHelper;
using IdentityService.Contracts;
using IdentityService.Validation;

namespace IdentityService.Tests;

public class ValidatorTests
{
    [Fact]
    public void Login_RequiresUsernameAndPassword()
    {
        var result = new LoginRequestValidator().TestValidate(new LoginRequest("", ""));

        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Login_AcceptsNormalInput()
    {
        new LoginRequestValidator().TestValidate(new LoginRequest("alice", "pw")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    public void Register_RejectsBadUsernames(string username)
    {
        var result = new RegisterRequestValidator()
            .TestValidate(new RegisterRequest(username, "long-enough-password", "CASHIER"));

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Register_RejectsShortPassword()
    {
        var result = new RegisterRequestValidator()
            .TestValidate(new RegisterRequest("alice", "short", "CASHIER"));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("GUEST")]
    [InlineData("")]
    [InlineData("1")]
    public void Register_RejectsUnknownRole(string role)
    {
        var result = new RegisterRequestValidator()
            .TestValidate(new RegisterRequest("alice", "long-enough-password", role));

        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Register_AcceptsValidRequest()
    {
        new RegisterRequestValidator()
            .TestValidate(new RegisterRequest("alice.smith", "long-enough-password", "kitchen"))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeRole_RejectsUnknownRole()
    {
        new ChangeRoleRequestValidator()
            .TestValidate(new ChangeRoleRequest("OWNER"))
            .ShouldHaveValidationErrorFor(x => x.Role);
    }
}
