namespace IdentityService.Domain;

public sealed class User
{
    public Guid Id { get; init; }

    /// <summary>Stored lower-case. Unique.</summary>
    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
