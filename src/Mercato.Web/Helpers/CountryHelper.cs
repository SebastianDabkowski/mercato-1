namespace Mercato.Web.Helpers;

/// <summary>
/// Provides helper methods for working with country codes and names.
/// </summary>
public static class CountryHelper
{
    private static readonly Dictionary<string, string> CountryNames = new()
    {
        { "US", "United States" },
        { "CA", "Canada" },
        { "GB", "United Kingdom" },
        { "AU", "Australia" },
        { "DE", "Germany" },
        { "FR", "France" },
        { "IT", "Italy" },
        { "ES", "Spain" },
        { "JP", "Japan" },
        { "BR", "Brazil" },
        { "MX", "Mexico" },
        { "IN", "India" },
        { "NL", "Netherlands" },
        { "BE", "Belgium" },
        { "CH", "Switzerland" },
        { "AT", "Austria" },
        { "SE", "Sweden" },
        { "NO", "Norway" },
        { "DK", "Denmark" },
        { "FI", "Finland" },
        { "PL", "Poland" },
        { "IE", "Ireland" },
        { "PT", "Portugal" },
        { "NZ", "New Zealand" },
        { "SG", "Singapore" },
        { "HK", "Hong Kong" },
        { "KR", "South Korea" },
        { "TW", "Taiwan" }
    };

    /// <summary>
    /// Gets the display name for a country code.
    /// </summary>
    /// <param name="countryCode">The ISO country code.</param>
    /// <returns>The country display name, or the code if no name is found.</returns>
    public static string GetCountryName(string countryCode)
    {
        return CountryNames.GetValueOrDefault(countryCode, countryCode);
    }
}
