using IdentityService.Contracts;
using IdentityService.Options;
using IdentityService.Security;
using IdentityService.Services;
using IdentityService.Validation;
using Microsoft.Extensions.Options;

namespace IdentityService.Endpoints;

public static class AuthPolicies
{
    public const string Admin = "Admin";
}

public static class AuthEndpoints
{
    public const string RefreshCookieName = "refresh_token";
    private const string CookiePath = "/api/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost("/refresh", RefreshAsync);

        group.MapPost("/register", RegisterAsync)
            .RequireAuthorization(AuthPolicies.Admin)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group.MapGet("/users", ListUsersAsync)
            .RequireAuthorization(AuthPolicies.Admin);

        group.MapPut("/users/{id:guid}/role", ChangeRoleAsync)
            .RequireAuthorization(AuthPolicies.Admin)
            .AddEndpointFilter<ValidationFilter<ChangeRoleRequest>>();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, AuthService auth, HttpContext http, IOptions<JwtOptions> jwt, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request.Username, request.Password, GetRequestInfo(http), ct);
        return ToAuthResult(result, http, jwt.Value);
    }

    private static async Task<IResult> RefreshAsync(
        AuthService auth, HttpContext http, IOptions<JwtOptions> jwt, CancellationToken ct)
    {
        if (!http.Request.Cookies.TryGetValue(RefreshCookieName, out var token) || string.IsNullOrWhiteSpace(token))
        {
            return Results.Json(Api.Fail("Invalid or expired session."), statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await auth.RefreshAsync(token, ct);
        return ToAuthResult(result, http, jwt.Value);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, AuthService auth, HttpContext http, CancellationToken ct)
    {
        if (!TryGetActorId(http, out var actorId))
        {
            return Results.Unauthorized();
        }

        var result = await auth.RegisterAsync(actorId, request, GetRequestInfo(http), ct);
        return ToResult(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> ListUsersAsync(
        AuthService auth, CancellationToken ct, int page = 1, int pageSize = 20)
    {
        var result = await auth.ListUsersAsync(page, pageSize, ct);
        return Results.Json(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    private static async Task<IResult> ChangeRoleAsync(
        Guid id, ChangeRoleRequest request, AuthService auth, HttpContext http, CancellationToken ct)
    {
        if (!TryGetActorId(http, out var actorId))
        {
            return Results.Unauthorized();
        }

        var result = await auth.ChangeRoleAsync(actorId, id, request.Role, GetRequestInfo(http), ct);
        return ToResult(result, StatusCodes.Status200OK);
    }

    private static IResult ToAuthResult(ServiceResult<LoginResult> result, HttpContext http, JwtOptions jwt)
    {
        if (!result.Succeeded)
        {
            return ToResult(result, StatusCodes.Status200OK);
        }

        var login = result.Value!;
        http.Response.Cookies.Append(RefreshCookieName, login.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(jwt.RefreshTokenDays)
        });

        // The refresh token is only ever in the cookie, never in the JSON body.
        return Results.Json(ApiResponse<AuthResponse>.Ok(login.Response));
    }

    private static IResult ToResult<T>(ServiceResult<T> result, int successStatus)
    {
        if (result.Succeeded)
        {
            return Results.Json(ApiResponse<T>.Ok(result.Value!), statusCode: successStatus);
        }

        var status = result.Error switch
        {
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(Api.Fail(result.Message), statusCode: status);
    }

    private static RequestInfo GetRequestInfo(HttpContext http) =>
        new(http.Connection.RemoteIpAddress?.ToString(), http.Request.Headers.UserAgent.ToString());

    private static bool TryGetActorId(HttpContext http, out Guid actorId)
    {
        var subject = http.User.FindFirst(JwtClaims.Subject)?.Value;
        return Guid.TryParse(subject, out actorId);
    }
}
