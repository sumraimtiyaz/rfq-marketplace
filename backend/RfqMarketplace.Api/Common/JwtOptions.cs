namespace RfqMarketplace.Api.Common;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "RfqMarketplace";
    public string Audience { get; set; } = "RfqMarketplace";

    /// <summary>Signing key. Must be at least 32 characters; supplied via configuration/secrets.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 480;

    /// <summary>Name of the HTTP-only cookie the browser app authenticates with.</summary>
    public string CookieName { get; set; } = "rfq_token";

    /// <summary>Set false only for plain-HTTP local development.</summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>Lax when the API is same-site with the app; None (plus Secure) when they are on different domains.</summary>
    public string CookieSameSite { get; set; } = "Lax";

    /// <summary>Optional parent domain, e.g. ".example.com". Leave empty for a host-only cookie.</summary>
    public string? CookieDomain { get; set; }
}
