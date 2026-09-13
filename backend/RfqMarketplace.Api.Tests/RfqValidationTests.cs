using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using RfqMarketplace.Api.Tests.Infrastructure;

namespace RfqMarketplace.Api.Tests;

/// <summary>
/// The frontend validates too, but only the API is a security boundary - these requests bypass
/// the UI entirely, which is exactly what a hostile client would do.
/// </summary>
public class RfqValidationTests(RfqApiFactory factory) : IClassFixture<RfqApiFactory>
{
    private static object ValidPayload(
        string? productName = "Office Chairs",
        string? description = "Ergonomic mesh chairs.",
        int quantity = 100,
        string? location = "Ahmedabad",
        int deadlineOffsetDays = 14) => new
        {
            productName,
            description,
            quantity,
            deliveryLocation = location,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(deadlineOffsetDays).ToString("yyyy-MM-dd")
        };

    private async Task<ValidationProblemDetails> PostExpectingValidationError(object payload)
    {
        var buyer = await factory.RegisterBuyerAsync();
        var response = await buyer.Client.PostAsJsonAsync("/api/rfqs", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        return await response.ReadAsync<ValidationProblemDetails>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(-1)]
    public async Task Quantity_MustBeGreaterThanZero(int quantity)
    {
        var problem = await PostExpectingValidationError(ValidPayload(quantity: quantity));

        problem.Errors.Should().ContainKey("Quantity");
        problem.Errors["Quantity"].Should().Contain("Quantity must be greater than 0.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ProductName_IsRequired(string? productName)
    {
        var problem = await PostExpectingValidationError(ValidPayload(productName: productName));
        problem.Errors.Should().ContainKey("ProductName");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Description_IsRequired(string? description)
    {
        var problem = await PostExpectingValidationError(ValidPayload(description: description));
        problem.Errors.Should().ContainKey("Description");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task DeliveryLocation_IsRequired(string? location)
    {
        var problem = await PostExpectingValidationError(ValidPayload(location: location));
        problem.Errors.Should().ContainKey("DeliveryLocation");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public async Task Deadline_CannotBeInThePast(int offsetDays)
    {
        var problem = await PostExpectingValidationError(ValidPayload(deadlineOffsetDays: offsetDays));

        problem.Errors.Should().ContainKey("Deadline");
        problem.Errors["Deadline"].Should().Contain("Deadline must be today or a future date.");
    }

    [Fact]
    public async Task Deadline_Today_IsAccepted()
    {
        // Today is still a usable deadline - suppliers have the rest of the day to respond.
        var buyer = await factory.RegisterBuyerAsync();

        var response = await buyer.Client.PostAsJsonAsync("/api/rfqs", ValidPayload(deadlineOffsetDays: 0));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ProductName_LongerThan200Chars_IsRejected()
    {
        var problem = await PostExpectingValidationError(ValidPayload(productName: new string('x', 201)));
        problem.Errors.Should().ContainKey("ProductName");
    }

    [Fact]
    public async Task Description_LongerThan5000Chars_IsRejected()
    {
        var problem = await PostExpectingValidationError(ValidPayload(description: new string('x', 5001)));
        problem.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public async Task AllFieldErrors_AreReportedTogether()
    {
        // One round trip should tell the user everything that is wrong, not just the first problem.
        var problem = await PostExpectingValidationError(new
        {
            productName = "",
            description = "",
            quantity = -5,
            deliveryLocation = "",
            deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3).ToString("yyyy-MM-dd")
        });

        problem.Errors.Keys.Should().Contain(["ProductName", "Description", "Quantity", "DeliveryLocation", "Deadline"]);
    }

    [Fact]
    public async Task InputIsTrimmedBeforeStorage()
    {
        var buyer = await factory.RegisterBuyerAsync();

        var response = await buyer.Client.PostAsJsonAsync("/api/rfqs", ValidPayload(
            productName: "  Padded Product  ", location: "  Ahmedabad  "));

        response.EnsureSuccessStatusCode();
        var rfq = await response.ReadAsync<Api.DTOs.Rfqs.RfqDetailDto>();

        rfq.ProductName.Should().Be("Padded Product");
        rfq.DeliveryLocation.Should().Be("Ahmedabad");
    }
}
