namespace Pos.Shared.Http;

/// <summary>Upper-case wire and database form of enums: PENDING, AVAILABLE, and so on.</summary>
public static class EnumWire
{
    public static string ToWire<T>(this T value) where T : struct, Enum => value.ToString().ToUpperInvariant();

    public static bool TryParseWire<T>(string? text, out T value) where T : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();

        // Reject numeric input such as "1" that Enum.TryParse would otherwise accept.
        return trimmed.All(char.IsLetter)
            && Enum.TryParse(trimmed, ignoreCase: true, out value)
            && Enum.IsDefined(value);
    }
}
