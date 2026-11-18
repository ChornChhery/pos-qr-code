namespace IdentityService.Options;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminUsername { get; set; } = "admin";

    /// <summary>Supplied through the Seed__AdminPassword environment variable. Never commit it.</summary>
    public string? AdminPassword { get; set; }
}
