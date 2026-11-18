namespace IdentityService.Security;

/// <summary>Claim names used in access and refresh tokens. The gateway uses the same names.</summary>
public static class JwtClaims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Role = "role";
    public const string TokenId = "jti";
}
