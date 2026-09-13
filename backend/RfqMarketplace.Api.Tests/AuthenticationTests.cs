using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using RfqMarketplace.Api.DTOs.Auth;
using RfqMarketplace.Api.Models;
using RfqMarketplace.Api.Tests.Infrastructure;

namespace RfqMarketplace.Api.Tests;

public class AuthenticationTests(RfqApiFactory factory) : IClassFixture<RfqApiFactory>
{
    [Theory]
    [InlineData(UserRole.Buyer)]
    [InlineData(UserRole.Supplier)]
    public async Task Register_WithValidPayload_CreatesAccountInRequestedRole(string role)
    {
        var user = await factory.RegisterAsync(role);

        user.User.Role.Should().Be(role);
        user.User.Id.Should().NotBeEmpty();
        user.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_SetsHttpOnlyAuthCookie()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Cookie Tester",
            companyName = "Cookie Co",
            email = ApiClient.NewEmail("cookie"),
            password = ApiClient.ValidPassword,
            role = UserRole.Buyer
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("rfq_token="));
        setCookie.Should().Contain("httponly", "the token must be unreadable from JavaScript");
        setCookie.Should().Contain("path=/");
    }

    [Fact]
    public async Task AuthCookie_AloneAuthenticatesSubsequentRequests()
    {
        // The browser app never sends an Authorization header, so the cookie must be enough.
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Cookie Only",
            companyName = "Cookie Only Co",
            email = ApiClient.NewEmail("cookieonly"),
            password = ApiClient.ValidPassword,
            role = UserRole.Buyer
        });

        var me = await client.GetAsync("/api/auth/me");

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await me.ReadAsync<UserDto>()).Role.Should().Be(UserRole.Buyer);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = ApiClient.NewEmail("dupe");
        var client = factory.CreateClient();
        object payload = new
        {
            name = "First",
            companyName = "First Co",
            email,
            password = ApiClient.ValidPassword,
            role = UserRole.Buyer
        };

        (await client.PostAsJsonAsync("/api/auth/register", payload)).EnsureSuccessStatusCode();
        var second = await client.PostAsJsonAsync("/api/auth/register", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("short", "Password must be at least 8 characters.")]
    [InlineData("alllowercase1", "Password must contain an uppercase letter.")]
    [InlineData("ALLUPPERCASE1", "Password must contain a lowercase letter.")]
    [InlineData("NoDigitsHere", "Password must contain a number.")]
    public async Task Register_WithWeakPassword_Returns400WithFieldError(string password, string expectedMessage)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            name = "Weak",
            companyName = "Weak Co",
            email = ApiClient.NewEmail("weak"),
            password,
            role = UserRole.Buyer
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.ReadAsync<ValidationProblemDetails>();
        problem.Errors.Should().ContainKey("Password");
        problem.Errors["Password"].Should().Contain(expectedMessage);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public async Task Register_WithInvalidEmail_Returns400(string email)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            name = "Bad Email",
            companyName = "Bad Email Co",
            email,
            password = ApiClient.ValidPassword,
            role = UserRole.Buyer
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("buyer")]
    [InlineData("")]
    public async Task Register_WithUnknownRole_Returns400(string role)
    {
        // Role is client-supplied, so it must be an allowlist, not a trusted string.
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            name = "Role Probe",
            companyName = "Role Probe Co",
            email = ApiClient.NewEmail("role"),
            password = ApiClient.ValidPassword,
            role
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ValidationProblemDetails>())
            .Errors.Should().ContainKey("Role");
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenAndRole()
    {
        var buyer = await factory.RegisterBuyerAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            email = buyer.Email,
            password = ApiClient.ValidPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.ReadAsync<AuthResponse>();
        auth.User.Role.Should().Be(UserRole.Buyer);
        auth.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var buyer = await factory.RegisterBuyerAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            email = buyer.Email,
            password = "WrongPassword123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        // Identical responses stop the endpoint doubling as an account-enumeration oracle.
        var buyer = await factory.RegisterBuyerAsync();
        var client = factory.CreateClient();

        var unknown = await client.PostAsJsonAsync("/api/auth/login",
            new { email = ApiClient.NewEmail("ghost"), password = ApiClient.ValidPassword });
        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login",
            new { email = buyer.Email, password = "WrongPassword123" });

        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var unknownDetail = (await unknown.ReadAsync<ProblemDetails>()).Detail;
        var wrongDetail = (await wrongPassword.ReadAsync<ProblemDetails>()).Detail;
        unknownDetail.Should().Be(wrongDetail);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var client = factory.CreateClient();

        // Reverse the signature segment: the payload is unchanged but the HMAC no longer matches.
        //
        // Flipping only the LAST character would not be enough. An HMAC-SHA256 signature is 32
        // bytes encoded as 43 base64url characters, and 43 characters carry 258 bits - so the
        // final character's low 4 bits are padding that decodes to nothing. 'A' and 'B' in that
        // position produce identical signature bytes, and the token still validates.
        var parts = buyer.Token.Split('.');
        var tamperedSignature = new string(parts[2].Reverse().ToArray());
        tamperedSignature.Should().NotBe(parts[2], "the tampering has to actually change something");

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {parts[0]}.{parts[1]}.{tamperedSignature}");

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PasswordIsNeverStoredInPlainText()
    {
        var buyer = await factory.RegisterBuyerAsync();

        await factory.WithDbAsync(async db =>
        {
            var stored = await Task.FromResult(db.Users.Single(u => u.Id == buyer.Id));
            stored.PasswordHash.Should().NotBeNullOrEmpty();
            stored.PasswordHash.Should().NotContain(ApiClient.ValidPassword);
        });
    }
}
