using System.Security.Claims;
using System.Text;
using FluentAssertions;
using IdentityService.Domain;
using IdentityService.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Tests;

public class TokenServiceTests
{
    private static readonly User Staff = new()
    {
        Id = Guid.Parse("0197a3b0-0000-7000-8000-000000000001"),
        Username = "alice",
        PasswordHash = "x",
        Role = UserRole.Manager
    };

    private readonly TokenService _sut = TestSupport.NewTokenService();

    [Fact]
    public void CreateAccessToken_ContainsSubjectNameAndRoleClaims()
    {
        var token = _sut.CreateAccessToken(Staff);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.Value);
        jwt.GetClaim(JwtClaims.Subject).Value.Should().Be(Staff.Id.ToString());
        jwt.GetClaim(JwtClaims.Name).Value.Should().Be("alice");
        jwt.GetClaim(JwtClaims.Role).Value.Should().Be("MANAGER");
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("test-audience");
    }

    [Fact]
    public void CreateAccessToken_ExpiresInFifteenMinutes()
    {
        var token = _sut.CreateAccessToken(Staff);

        token.ExpiresInSeconds.Should().Be(15 * 60);
    }

    [Fact]
    public async Task ValidateRefreshToken_ReturnsUserIdForFreshToken()
    {
        var refresh = _sut.CreateRefreshToken(Staff);

        var userId = await _sut.ValidateRefreshTokenAsync(refresh);

        userId.Should().Be(Staff.Id);
    }

    [Fact]
    public async Task ValidateRefreshToken_RejectsAccessToken()
    {
        var access = _sut.CreateAccessToken(Staff).Value;

        (await _sut.ValidateRefreshTokenAsync(access)).Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_IsRejectedWhenUsedAsAccessToken()
    {
        var refresh = _sut.CreateRefreshToken(Staff);
        var options = TestSupport.NewJwtOptions();

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(refresh, new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey))
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRefreshToken_RejectsGarbage()
    {
        (await _sut.ValidateRefreshTokenAsync("not-a-token")).Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshToken_RejectsTokenSignedWithAnotherKey()
    {
        var other = TestSupport.NewJwtOptions();
        other.SigningKey = "a-completely-different-signing-key-9876543210";
        var foreign = TestSupport.NewTokenService(other).CreateRefreshToken(Staff);

        (await _sut.ValidateRefreshTokenAsync(foreign)).Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshToken_RejectsExpiredToken()
    {
        var options = TestSupport.NewJwtOptions();
        var now = DateTime.UtcNow;
        var expired = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.RefreshAudience,
            Subject = new ClaimsIdentity(new[] { new Claim(JwtClaims.Subject, Staff.Id.ToString()) }),
            NotBefore = now.AddHours(-3),
            IssuedAt = now.AddHours(-3),
            Expires = now.AddHours(-2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        });

        (await _sut.ValidateRefreshTokenAsync(expired)).Should().BeNull();
    }
}
