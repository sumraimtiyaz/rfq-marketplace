using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RfqMarketplace.Api.DTOs.Auth;
using RfqMarketplace.Api.DTOs.Quotations;
using RfqMarketplace.Api.DTOs.Rfqs;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Tests.Infrastructure;

/// <summary>Test-side helpers for registering users and driving the API as one of them.</summary>
public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static int _counter;

    /// <summary>Unique per call, so tests never collide on the unique email index.</summary>
    public static string NewEmail(string prefix = "user") =>
        $"{prefix}-{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@test.local";

    public const string ValidPassword = "Password123";

    /// <summary>Registers a user and returns a client that authenticates as them via bearer token.</summary>
    public static async Task<TestUser> RegisterAsync(
        this RfqApiFactory factory, string role, string? company = null)
    {
        var client = factory.CreateClient();
        var email = NewEmail(role.ToLowerInvariant());

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = $"Test {role}",
            companyName = company ?? $"{role} Co {Guid.NewGuid().ToString("N")[..6]}",
            email,
            password = ValidPassword,
            role
        });

        response.EnsureSuccessStatusCode();
        var auth = await response.ReadAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new TestUser(client, auth.User, auth.Token, email);
    }

    public static Task<TestUser> RegisterBuyerAsync(this RfqApiFactory factory, string? company = null)
        => factory.RegisterAsync(UserRole.Buyer, company);

    public static Task<TestUser> RegisterSupplierAsync(this RfqApiFactory factory, string? company = null)
        => factory.RegisterAsync(UserRole.Supplier, company);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<T>(Json)
           ?? throw new InvalidOperationException($"Response body was null. Status: {response.StatusCode}");

    /// <summary>Creates an RFQ as the given buyer and returns it. Fails loudly if the API rejects it.</summary>
    public static async Task<RfqDetailDto> CreateRfqAsync(
        this TestUser buyer,
        string productName = "Ergonomic Office Chairs",
        string description = "Mesh-back ergonomic chairs with adjustable lumbar support.",
        int quantity = 500,
        string location = "Ahmedabad",
        int deadlineInDays = 21)
    {
        var response = await buyer.Client.PostAsJsonAsync("/api/rfqs", new
        {
            productName,
            description,
            quantity,
            deliveryLocation = location,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(deadlineInDays).ToString("yyyy-MM-dd")
        });

        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<RfqDetailDto>();
    }

    public static Task<HttpResponseMessage> SubmitQuotationAsync(
        this TestUser supplier, Guid rfqId, decimal price = 125_000m, int days = 15, string? message = "Ready to supply.")
        => supplier.Client.PostAsJsonAsync($"/api/rfqs/{rfqId}/quotations", new
        {
            quotedPrice = price,
            estimatedDeliveryDays = days,
            message
        });

    public static async Task<QuotationDto> SubmitQuotationOrThrowAsync(
        this TestUser supplier, Guid rfqId, decimal price = 125_000m, int days = 15)
    {
        var response = await supplier.SubmitQuotationAsync(rfqId, price, days);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<QuotationDto>();
    }
}

public record TestUser(HttpClient Client, UserDto User, string Token, string Email)
{
    public Guid Id => User.Id;
}
