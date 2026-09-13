namespace RfqMarketplace.Api.Common;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Reads a boolean setting, treating an absent, blank or unparseable value as the default.
    ///
    /// <c>GetValue&lt;bool&gt;</c> throws on an empty string: a section whose value is "" is not
    /// null, so the binder tries to parse it and a FormatException takes the whole app down at
    /// startup. Hosting dashboards make that easy to hit - declaring an environment variable and
    /// leaving the box blank stores an empty string, not nothing. A deployment should not fail to
    /// boot over an optional feature flag, so this falls back instead.
    ///
    /// Accepts "true"/"false" in any casing, plus "1"/"0", which platforms and people both use.
    /// </summary>
    public static bool GetBool(this IConfiguration configuration, string key, bool defaultValue)
    {
        var raw = configuration[key];

        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        var value = raw.Trim();

        if (bool.TryParse(value, out var parsed))
            return parsed;

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => defaultValue
        };
    }
}
