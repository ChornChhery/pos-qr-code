using System.Text.Json;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Security;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

/// <summary>Information about the HTTP caller, stored in the audit log.</summary>
public sealed record RequestInfo(string? IpAddress, string? UserAgent);

/// <summary>The API response plus the refresh token that the endpoint puts in an HttpOnly cookie.</summary>
public sealed record LoginResult(AuthResponse Response, string RefreshToken);

public sealed class AuthService(
    IdentityDbContext db,
    IPasswordService passwords,
    ITokenService tokens,
    TimeProvider clock,
    ILogger<AuthService> logger)
{
    public const string InvalidCredentialsMessage = "Invalid username or password.";

    public static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    public async Task<ServiceResult<LoginResult>> LoginAsync(
        string username, string password, RequestInfo info, CancellationToken ct = default)
    {
        var normalized = NormalizeUsername(username);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalized, ct);

        if (user is null)
        {
            passwords.BurnVerify(password);
            return await FailLoginAsync(null, "unknown_user", info, ct);
        }

        if (!passwords.Verify(user.PasswordHash, password))
        {
            return await FailLoginAsync(user.Id, "bad_password", info, ct);
        }

        // Checked after the password so that account state is not revealed to a guesser.
        if (!user.IsActive)
        {
            return await FailLoginAsync(user.Id, "inactive", info, ct);
        }

        AddAudit(user.Id, "LOGIN_SUCCESS", null, info);
        await db.SaveChangesAsync(ct);

        return ServiceResult<LoginResult>.Ok(BuildLoginResult(user));
    }

    public async Task<ServiceResult<LoginResult>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = await tokens.ValidateRefreshTokenAsync(refreshToken);
        if (userId is null)
        {
            return ServiceResult<LoginResult>.Fail(ErrorKind.Unauthorized, "Invalid or expired session.");
        }

        // The role is read from the database, so role changes apply at the next refresh.
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user is null || !user.IsActive)
        {
            return ServiceResult<LoginResult>.Fail(ErrorKind.Unauthorized, "Invalid or expired session.");
        }

        return ServiceResult<LoginResult>.Ok(BuildLoginResult(user));
    }

    public async Task<ServiceResult<UserDto>> RegisterAsync(
        Guid actorId, RegisterRequest request, RequestInfo info, CancellationToken ct = default)
    {
        if (!UserRoleExtensions.TryParseWire(request.Role, out var role))
        {
            return ServiceResult<UserDto>.Fail(ErrorKind.Validation, "Unknown role.");
        }

        var username = NormalizeUsername(request.Username);
        if (await db.Users.AnyAsync(u => u.Username == username, ct))
        {
            return ServiceResult<UserDto>.Fail(ErrorKind.Conflict, "Username already exists.");
        }

        var now = clock.GetUtcNow();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            PasswordHash = passwords.Hash(request.Password),
            Role = role,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(user);
        AddAudit(actorId, "USER_REGISTERED", new { targetUserId = user.Id, role = role.ToWire() }, info);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Two admins registering the same username at once: the unique index decides.
            logger.LogWarning(ex, "User registration failed to save.");
            return ServiceResult<UserDto>.Fail(ErrorKind.Conflict, "Username already exists.");
        }

        return ServiceResult<UserDto>.Ok(ToDto(user));
    }

    public async Task<PagedResult<UserDto>> ListUsersAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.Users.CountAsync(ct);
        var items = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.Username, u.Role, u.IsActive, u.CreatedAt })
            .ToListAsync(ct);

        var dtos = items
            .Select(u => new UserDto(u.Id, u.Username, u.Role.ToWire(), u.IsActive, u.CreatedAt))
            .ToList();

        return new PagedResult<UserDto>(dtos, page, pageSize, total);
    }

    public async Task<ServiceResult<UserDto>> ChangeRoleAsync(
        Guid actorId, Guid targetId, string roleValue, RequestInfo info, CancellationToken ct = default)
    {
        if (!UserRoleExtensions.TryParseWire(roleValue, out var role))
        {
            return ServiceResult<UserDto>.Fail(ErrorKind.Validation, "Unknown role.");
        }

        if (actorId == targetId)
        {
            return ServiceResult<UserDto>.Fail(ErrorKind.Forbidden, "You cannot change your own role.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == targetId, ct);
        if (user is null)
        {
            return ServiceResult<UserDto>.Fail(ErrorKind.NotFound, "User not found.");
        }

        var previous = user.Role;
        user.Role = role;
        user.UpdatedAt = clock.GetUtcNow();

        AddAudit(actorId, "USER_ROLE_CHANGED",
            new { targetUserId = user.Id, from = previous.ToWire(), to = role.ToWire() }, info);

        await db.SaveChangesAsync(ct);
        return ServiceResult<UserDto>.Ok(ToDto(user));
    }

    private LoginResult BuildLoginResult(User user)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken(user);
        var response = new AuthResponse(access.Value, "Bearer", access.ExpiresInSeconds, ToDto(user));
        return new LoginResult(response, refresh);
    }

    private async Task<ServiceResult<LoginResult>> FailLoginAsync(
        Guid? userId, string reason, RequestInfo info, CancellationToken ct)
    {
        AddAudit(userId, "LOGIN_FAILED", new { reason }, info);
        await db.SaveChangesAsync(ct);
        return ServiceResult<LoginResult>.Fail(ErrorKind.Unauthorized, InvalidCredentialsMessage);
    }

    private void AddAudit(Guid? userId, string action, object? details, RequestInfo info)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Action = action,
            Details = details is null ? null : JsonSerializer.Serialize(details),
            IpAddress = Truncate(info.IpAddress, 45),
            UserAgent = Truncate(info.UserAgent, 256),
            CreatedAt = clock.GetUtcNow()
        });
    }

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Username, user.Role.ToWire(), user.IsActive, user.CreatedAt);
}
