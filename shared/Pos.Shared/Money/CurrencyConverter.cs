namespace Pos.Shared.Money;

public sealed class CurrencyOptions
{
    public const string SectionName = "Currency";

    /// <summary>Riel per 1 USD. Every service that shows riel prices must use the same value.</summary>
    public decimal UsdToKhrRate { get; set; } = 4000m;
}

/// <summary>
/// All money is stored in USD with two decimals. Riel is a display value computed from the USD amount
/// and rounded to the nearest 100 riel, the smallest practical denomination.
/// </summary>
public static class CurrencyConverter
{
    public const int KhrRounding = 100;

    public static decimal RoundUsd(decimal usd) => Math.Round(usd, 2, MidpointRounding.AwayFromZero);

    public static long ToKhr(decimal usd, decimal usdToKhrRate)
    {
        if (usd <= 0m || usdToKhrRate <= 0m)
        {
            return 0;
        }

        var steps = Math.Round(usd * usdToKhrRate / KhrRounding, 0, MidpointRounding.AwayFromZero);
        return (long)steps * KhrRounding;
    }
}
