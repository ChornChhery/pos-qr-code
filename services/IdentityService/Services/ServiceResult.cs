namespace IdentityService.Services;

public enum ErrorKind
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

public sealed record ServiceResult<T>(bool Succeeded, T? Value, ErrorKind Error, string Message)
{
    public static ServiceResult<T> Ok(T value) => new(true, value, ErrorKind.None, string.Empty);

    public static ServiceResult<T> Fail(ErrorKind kind, string message) => new(false, default, kind, message);
}
