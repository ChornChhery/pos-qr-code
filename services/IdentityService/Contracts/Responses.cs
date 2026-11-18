namespace IdentityService.Contracts;

public sealed record UserDto(Guid Id, string Username, string Role, bool IsActive, DateTimeOffset CreatedAt);

public sealed record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, UserDto User);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
