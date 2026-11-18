using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using ApiGateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // Structured JSON logs outside local development.
    builder.Logging.AddJsonConsole();
}

// ---- JWT pre-validation (same issuer, audience, and key as the Identity Service) ----
builder.Services
    .AddOptions<GatewayJwtOptions>()
    .Bind(builder.Configuration.GetSection(GatewayJwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<GatewayJwtOptions>>((bearer, jwtOptions) =>
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
    });

// YARP also offers the built-in "default" and "anonymous" policies.
builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());

// ---- CORS: explicit origins only, credentials allowed for the refresh-token cookie ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowCredentials()
        .WithHeaders("Authorization", "Content-Type", "X-Requested-With", "X-SignalR-User-Agent")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")));

// ---- Rate limiting (per client IP) ----
// Restaurants share one Wi-Fi IP between many guests, so the public limits are deliberately generous.
// Tune them in Phase 6 with real load numbers.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            data = (object?)null,
            message = "Too many requests. Please try again shortly.",
            errors = Array.Empty<string>()
        }, ct);
    };

    options.AddPolicy("general", http => PerIp(http, permitLimit: 300));
    options.AddPolicy("login", http => PerIp(http, permitLimit: 10));
    options.AddPolicy("public-write", http => PerIp(http, permitLimit: 30));
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new
    {
        success = false,
        data = (object?)null,
        message = "An unexpected error occurred.",
        errors = Array.Empty<string>()
    });
}));

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));
app.MapReverseProxy();

// Fail fast on bad JWT settings.
_ = app.Services.GetRequiredService<IOptions<GatewayJwtOptions>>().Value;

app.Run();

static RateLimitPartition<string> PerIp(HttpContext http, int permitLimit) =>
    RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
