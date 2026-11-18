namespace IdentityService.Domain;

/// <summary>Staff roles. Customers are unauthenticated guests and never appear here.</summary>
public enum UserRole
{
    Cashier,
    Kitchen,
    Manager,
    Admin
}

public static class UserRoleExtensions
{
    /// <summary>Wire/database representation: CASHIER, KITCHEN, MANAGER, ADMIN.</summary>
    public static string ToWire(this UserRole role) => role.ToString().ToUpperInvariant();

    public static bool TryParseWire(string? value, out UserRole role)
    {
        role = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        // Reject numeric input such as "1" that Enum.TryParse would otherwise accept.
        return trimmed.All(char.IsLetter)
            && Enum.TryParse(trimmed, ignoreCase: true, out role)
            && Enum.IsDefined(role);
    }
}
