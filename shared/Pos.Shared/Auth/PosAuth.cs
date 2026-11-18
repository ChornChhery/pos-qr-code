using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Pos.Shared.Auth;

/// <summary>Role names as they appear in the JWT "role" claim.</summary>
public static class Roles
{
    public const string Cashier = "CASHIER";
    public const string Kitchen = "KITCHEN";
    public const string Manager = "MANAGER";
    public const string Admin = "ADMIN";
}

public static class Policies
{
    /// <summary>ADMIN or MANAGER: menu management.</summary>
    public const string MenuManagers = "MenuManagers";

    /// <summary>KITCHEN, MANAGER, or ADMIN: order status changes.</summary>
    public const string OrderStatusUpdaters = "OrderStatusUpdaters";

    /// <summary>Any logged-in staff role.</summary>
    public const string AnyStaff = "AnyStaff";

    /// <summary>KITCHEN, MANAGER, or ADMIN: kitchen live hub.</summary>
    public const string KitchenHub = "KitchenHub";

    /// <summary>CASHIER, MANAGER, or ADMIN: cashier live hub.</summary>
    public const string CashierHub = "CashierHub";
}

/// <summary>Validation settings for access tokens issued by the Identity Service. Same values as the gateway.</summary>
public sealed class PosJwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Supplied through the Jwt__SigningKey environment variable. At least 32 characters.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;
}

public static class PosAuthExtensions
{
    /// <summary>
    /// Adds JWT bearer validation and the role policies. When <paramref name="allowQueryToken"/> is true, a token in the
    /// "access_token" query parameter is accepted for requests under /hubs (browsers cannot set headers on WebSockets).
    /// </summary>
    public static IServiceCollection AddPosAuthentication(
        this IServiceCollection services, IConfiguration configuration, bool allowQueryToken = false)
    {
        services
            .AddOptions<PosJwtOptions>()
            .Bind(configuration.GetSection(PosJwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<PosJwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                if (allowQueryToken)
                {
                    bearer.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                }
            });

        services
            .AddAuthorizationBuilder()
            .AddPolicy(Policies.MenuManagers, p => p.RequireRole(Roles.Admin, Roles.Manager))
            .AddPolicy(Policies.OrderStatusUpdaters, p => p.RequireRole(Roles.Kitchen, Roles.Manager, Roles.Admin))
            .AddPolicy(Policies.AnyStaff, p => p.RequireRole(Roles.Cashier, Roles.Kitchen, Roles.Manager, Roles.Admin))
            .AddPolicy(Policies.KitchenHub, p => p.RequireRole(Roles.Kitchen, Roles.Manager, Roles.Admin))
            .AddPolicy(Policies.CashierHub, p => p.RequireRole(Roles.Cashier, Roles.Manager, Roles.Admin));

        return services;
    }
}
