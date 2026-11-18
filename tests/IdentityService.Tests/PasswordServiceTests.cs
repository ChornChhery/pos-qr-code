using FluentAssertions;
using IdentityService.Security;

namespace IdentityService.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void Hash_DoesNotContainThePlainPassword()
    {
        var hash = _sut.Hash("correct-horse-battery");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotContain("correct-horse-battery");
    }

    [Fact]
    public void Hash_IsSaltedSoTwoHashesDiffer()
    {
        _sut.Hash("same-password-123").Should().NotBe(_sut.Hash("same-password-123"));
    }

    [Fact]
    public void Verify_AcceptsCorrectPassword()
    {
        var hash = _sut.Hash("correct-horse-battery");

        _sut.Verify(hash, "correct-horse-battery").Should().BeTrue();
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        var hash = _sut.Hash("correct-horse-battery");

        _sut.Verify(hash, "wrong-password-123").Should().BeFalse();
    }

    [Fact]
    public void BurnVerify_DoesNotThrow()
    {
        var act = () => _sut.BurnVerify("anything");

        act.Should().NotThrow();
    }
}
