namespace IdentityService.Domain;

public sealed class AuditLog
{
    public Guid Id { get; init; }

    /// <summary>The acting user, or null when the actor is unknown (for example a failed login).</summary>
    public Guid? UserId { get; init; }

    public required string Action { get; init; }

    /// <summary>JSON document stored as jsonb. Never put passwords, tokens, or slips here.</summary>
    public string? Details { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
