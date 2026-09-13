using FluentAssertions;
using Microsoft.Extensions.Configuration;
using RfqMarketplace.Api.Common;

namespace RfqMarketplace.Api.Tests;

/// <summary>
/// Regression cover for a real deployment failure: a hosting dashboard stored
/// Swagger__Enabled as an empty string, and GetValue&lt;bool&gt; threw a FormatException
/// that took the whole app down before it finished starting. An optional feature flag
/// must never be able to do that.
/// </summary>
public class ConfigurationTests
{
    private static IConfiguration Build(params (string Key, string? Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBool_WithBlankValue_FallsBackToDefault(string value)
    {
        // The case that broke production: declared in the dashboard, left empty.
        var configuration = Build(("Feature:Flag", value));

        configuration.GetBool("Feature:Flag", defaultValue: true).Should().BeTrue();
        configuration.GetBool("Feature:Flag", defaultValue: false).Should().BeFalse();
    }

    [Fact]
    public void GetBool_WithMissingKey_FallsBackToDefault()
    {
        var configuration = Build();

        configuration.GetBool("Nothing:Here", defaultValue: true).Should().BeTrue();
        configuration.GetBool("Nothing:Here", defaultValue: false).Should().BeFalse();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData(" true ", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("0", false)]
    public void GetBool_ParsesTheSpellingsPlatformsActuallyUse(string value, bool expected)
    {
        Build(("Feature:Flag", value))
            .GetBool("Feature:Flag", defaultValue: !expected)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("enabled")]
    [InlineData("banana")]
    public void GetBool_WithUnparseableValue_FallsBackInsteadOfThrowing(string value)
    {
        var configuration = Build(("Feature:Flag", value));

        // Deliberate: a typo in a feature flag should not stop the service booting.
        configuration.Invoking(c => c.GetBool("Feature:Flag", defaultValue: true))
            .Should().NotThrow();

        configuration.GetBool("Feature:Flag", defaultValue: true).Should().BeTrue();
    }
}
