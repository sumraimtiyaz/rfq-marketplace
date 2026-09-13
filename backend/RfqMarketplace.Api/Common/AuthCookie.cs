namespace RfqMarketplace.Api.Common;

/// <summary>
/// Writes and clears the authentication cookie.
///
/// The token is stored in an HTTP-only cookie rather than localStorage: JavaScript on the page
/// cannot read it, so an XSS bug cannot exfiltrate a valid session. The same token is also
/// returned in the response body for non-browser clients (Swagger, Postman, integration tests).
/// </summary>
public static class AuthCookie
{
    public static void SetAuthCookie(this HttpResponse response, JwtOptions options, string token, DateTimeOffset expiresAt)
        => response.Cookies.Append(options.CookieName, token, BuildOptions(options, expiresAt));

    public static void ClearAuthCookie(this HttpResponse response, JwtOptions options)
        => response.Cookies.Delete(options.CookieName, BuildOptions(options, expiresAt: null));

    private static CookieOptions BuildOptions(JwtOptions options, DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = options.CookieSecure,
        SameSite = ParseSameSite(options.CookieSameSite),
        Expires = expiresAt,
        Path = "/",
        Domain = string.IsNullOrWhiteSpace(options.CookieDomain) ? null : options.CookieDomain
    };

    /// <summary>
    /// Lax is right when the API and the app share a site. When they are on different domains the
    /// cookie has to be SameSite=None, which browsers only accept together with Secure.
    /// </summary>
    private static SameSiteMode ParseSameSite(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "none" => SameSiteMode.None,
        "strict" => SameSiteMode.Strict,
        _ => SameSiteMode.Lax
    };
}
