using System.Security.Claims;
using System.Text;
using IdentityService.Domain;
using IdentityService.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Security;

public sealed record AccessToken(string Value, int ExpiresInSeconds);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    string CreateRefreshToken(User user);

    /// <summary>Returns the user id from a valid refresh token, or null when it is invalid or expired.</summary>
    Task<Guid?> ValidateRefreshTokenAsync(string token);
}

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(options.Value.SigningKey));
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtClaims.Subject, user.Id.ToString()),
                new Claim(JwtClaims.Name, user.Username),
                new Claim(JwtClaims.Role, user.Role.ToWire()),
                new Claim(JwtClaims.TokenId, Guid.NewGuid().ToString("N"))
            }),
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_handler.CreateToken(descriptor), (int)(expires - now).TotalSeconds);
    }

    public string CreateRefreshToken(User user)
    {
        var now = clock.GetUtcNow();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.RefreshAudience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtClaims.Subject, user.Id.ToString()),
                new Claim(JwtClaims.TokenId, Guid.NewGuid().ToString("N"))
            }),
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = now.AddDays(_options.RefreshTokenDays).UtcDateTime,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        return _handler.CreateToken(descriptor);
    }

    public async Task<Guid?> ValidateRefreshTokenAsync(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.RefreshAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        var result = await _handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid)
        {
            return null;
        }

        var subject = result.ClaimsIdentity.FindFirst(JwtClaims.Subject)?.Value;
        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}
