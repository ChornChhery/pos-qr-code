using Microsoft.AspNetCore.Http;

namespace Pos.Shared.Http;

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

public enum ErrorKind
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

public sealed record ServiceResult<T>(
    bool Succeeded, T? Value, ErrorKind Error, string Message, IReadOnlyList<string> Errors)
{
    public static ServiceResult<T> Ok(T value) =>
        new(true, value, ErrorKind.None, string.Empty, Array.Empty<string>());

    public static ServiceResult<T> Fail(ErrorKind kind, string message, IReadOnlyList<string>? errors = null) =>
        new(false, default, kind, message, errors ?? Array.Empty<string>());
}

public static class ServiceResultHttp
{
    /// <summary>Maps a service result to an HTTP response in the standard envelope.</summary>
    public static IResult ToHttp<T>(this ServiceResult<T> result, int successStatus = StatusCodes.Status200OK)
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

        return Results.Json(Api.Fail(result.Message, result.Errors), statusCode: status);
    }
}
