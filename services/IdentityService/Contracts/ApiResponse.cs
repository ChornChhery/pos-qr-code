namespace IdentityService.Contracts;

/// <summary>Standard response envelope: { success, data, message, errors }.</summary>
public sealed record ApiResponse<T>(bool Success, T? Data, string Message, IReadOnlyList<string> Errors)
{
    public static ApiResponse<T> Ok(T data, string message = "") => new(true, data, message, Array.Empty<string>());
}

public static class Api
{
    public static ApiResponse<object> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new(false, null, message, errors ?? Array.Empty<string>());
}
