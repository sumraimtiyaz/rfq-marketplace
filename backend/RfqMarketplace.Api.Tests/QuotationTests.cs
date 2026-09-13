using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfqMarketplace.Api.DTOs.Quotations;
using RfqMarketplace.Api.Tests.Infrastructure;

namespace RfqMarketplace.Api.Tests;

public class QuotationTests(RfqApiFactory factory) : IClassFixture<RfqApiFactory>
{
    [Fact]
    public async Task Supplier_CanSubmitAQuotation()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync(company: "ABC Furniture");

        var response = await supplier.SubmitQuotationAsync(rfq.Id, price: 1_250_000m, days: 20);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var quotation = await response.ReadAsync<QuotationDto>();
        quotation.QuotedPrice.Should().Be(1_250_000m);
        quotation.EstimatedDeliveryDays.Should().Be(20);
        quotation.SupplierCompanyName.Should().Be("ABC Furniture");
    }

    [Fact]
    public async Task Supplier_CannotQuoteTwiceOnTheSameRfq()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();

        await supplier.SubmitQuotationOrThrowAsync(rfq.Id);
        var second = await supplier.SubmitQuotationAsync(rfq.Id, price: 999m);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.ReadAsync<ProblemDetails>()).Detail
            .Should().Be("You have already submitted a quotation for this RFQ.");
    }

    [Fact]
    public async Task ConcurrentDuplicateSubmissions_OnlyOneIsStored()
    {
        // The pre-check cannot win this race; the unique index on (RfqId, SupplierId) is what does.
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();

        var attempts = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(i => supplier.SubmitQuotationAsync(rfq.Id, price: 1000m + i)));

        attempts.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        attempts.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(4);

        await factory.WithDbAsync(async db =>
            (await db.Quotations.CountAsync(q => q.RfqId == rfq.Id)).Should().Be(1));
    }

    [Fact]
    public async Task DifferentSuppliers_CanEachQuoteTheSameRfq()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();

        foreach (var price in new[] { 100_000m, 110_000m, 120_000m })
        {
            var supplier = await factory.RegisterSupplierAsync();
            (await supplier.SubmitQuotationAsync(rfq.Id, price)).StatusCode
                .Should().Be(HttpStatusCode.Created);
        }

        var quotations = await (await buyer.Client.GetAsync($"/api/rfqs/{rfq.Id}/quotations"))
            .ReadAsync<List<QuotationDto>>();

        quotations.Should().HaveCount(3);
    }

    [Fact]
    public async Task Quotation_OnClosedRfq_IsRejected()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        (await buyer.Client.PostAsync($"/api/rfqs/{rfq.Id}/close", null)).EnsureSuccessStatusCode();

        var supplier = await factory.RegisterSupplierAsync();
        var response = await supplier.SubmitQuotationAsync(rfq.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ProblemDetails>()).Detail.Should().Contain("closed");
    }

    [Fact]
    public async Task Quotation_AfterDeadline_IsRejected()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();

        // Validation blocks creating an RFQ in the past, so age it directly in the database.
        await factory.WithDbAsync(async db =>
        {
            var stored = await db.Rfqs.SingleAsync(r => r.Id == rfq.Id);
            stored.Deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            await db.SaveChangesAsync();
        });

        var supplier = await factory.RegisterSupplierAsync();
        var response = await supplier.SubmitQuotationAsync(rfq.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ProblemDetails>()).Detail.Should().Contain("deadline");
    }

    [Fact]
    public async Task Quotation_OnMissingRfq_Returns404()
    {
        var supplier = await factory.RegisterSupplierAsync();

        var response = await supplier.SubmitQuotationAsync(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500.50)]
    public async Task QuotedPrice_MustBeGreaterThanZero(decimal price)
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();

        var response = await supplier.SubmitQuotationAsync(rfq.Id, price);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ValidationProblemDetails>())
            .Errors.Should().ContainKey("QuotedPrice");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(4000)]
    public async Task EstimatedDeliveryDays_MustBeSensible(int days)
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();

        var response = await supplier.SubmitQuotationAsync(rfq.Id, days: days);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ValidationProblemDetails>())
            .Errors.Should().ContainKey("EstimatedDeliveryDays");
    }

    [Fact]
    public async Task Message_IsOptional()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();

        var response = await supplier.Client.PostAsJsonAsync($"/api/rfqs/{rfq.Id}/quotations", new
        {
            quotedPrice = 50_000m,
            estimatedDeliveryDays = 10
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadAsync<QuotationDto>()).Message.Should().BeNull();
    }

    [Fact]
    public async Task Message_LongerThan2000Chars_IsRejected()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();

        var response = await supplier.SubmitQuotationAsync(rfq.Id, message: new string('x', 2001));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BuyerQuotationList_IsSortedCheapestFirst()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();

        foreach (var price in new[] { 132_000m, 900m, 118_000m })
        {
            var supplier = await factory.RegisterSupplierAsync();
            await supplier.SubmitQuotationOrThrowAsync(rfq.Id, price);
        }

        var quotations = await (await buyer.Client.GetAsync($"/api/rfqs/{rfq.Id}/quotations"))
            .ReadAsync<List<QuotationDto>>();

        quotations.Select(q => q.QuotedPrice).Should().BeInAscendingOrder();
        quotations[0].QuotedPrice.Should().Be(900m);
    }

    [Fact]
    public async Task DeletingAnRfq_RemovesItsQuotations()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();
        await supplier.SubmitQuotationOrThrowAsync(rfq.Id);

        (await buyer.Client.DeleteAsync($"/api/rfqs/{rfq.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        await factory.WithDbAsync(async db =>
            (await db.Quotations.CountAsync(q => q.RfqId == rfq.Id)).Should().Be(0));
    }
}
