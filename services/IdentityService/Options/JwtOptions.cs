using System.ComponentModel.DataAnnotations;

namespace IdentityService.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>HS256 key. At least 32 characters. Supplied through the Jwt__SigningKey environment variable.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// Refresh tokens use a different audience, so a refresh token is rejected as an access token.
    /// </summary>
    public string RefreshAudience => Audience + ".refresh";
}
