using System.ComponentModel.DataAnnotations;

namespace ApiGateway;

/// <summary>
/// Settings the gateway needs to validate access tokens issued by the Identity Service.
/// The signing key is shared and comes from the Jwt__SigningKey environment variable.
/// </summary>
public sealed class GatewayJwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;
}
