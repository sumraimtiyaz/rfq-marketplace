using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RfqMarketplace.Api.Tests.Infrastructure;

namespace RfqMarketplace.Api.Tests;

/// <summary>
/// Two distinct questions, tested separately:
///   1. Role authorization - is a Supplier allowed to call a Buyer endpoint at all?
///   2. Resource authorization - given the right role, does this record belong to this caller?
/// The second is the one that is easy to forget and easy to exploit.
/// </summary>
public class AuthorizationTests(RfqApiFactory factory) : IClassFixture<RfqApiFactory>
{
    // ---------------------------------------------------------------- role checks

    [Fact]
    public async Task Supplier_CannotCreateRfq()
    {
        var supplier = await factory.RegisterSupplierAsync();

        var response = await supplier.Client.PostAsJsonAsync("/api/rfqs", new
        {
            productName = "Sneaky RFQ",
            description = "A supplier should not be able to post this.",
            quantity = 10,
            deliveryLocation = "Ahmedabad",
            deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10).ToString("yyyy-MM-dd")
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Buyer_CannotBrowseSupplierMarketplace()
    {
        var buyer = await factory.RegisterBuyerAsync();
        (await buyer.Client.GetAsync("/api/rfqs")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Buyer_CannotSubmitQuotation()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();

        var response = await buyer.SubmitQuotationAsync(rfq.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Buyer_CannotListSupplierQuotations()
    {
        var buyer = await factory.RegisterBuyerAsync();
        (await buyer.Client.GetAsync("/api/quotations/my")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Supplier_CannotListBuyerRfqs()
    {
        var supplier = await factory.RegisterSupplierAsync();
        (await supplier.Client.GetAsync("/api/rfqs/my")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Supplier_CannotReadQuotationsOnAnRfq()
    {
        // Competitor pricing is the one thing a supplier must never see.
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();
        await supplier.SubmitQuotationOrThrowAsync(rfq.Id);

        var response = await supplier.Client.GetAsync($"/api/rfqs/{rfq.Id}/quotations");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousRequests_AreRejectedOnEveryProtectedEndpoint()
    {
        var client = factory.CreateClient();

        foreach (var path in new[] { "/api/rfqs", "/api/rfqs/my", "/api/quotations/my", "/api/dashboard/buyer" })
            (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "path {0} is protected", path);
    }

    // ------------------------------------------------------------ resource checks

    [Fact]
    public async Task Buyer_CannotReadAnotherBuyersRfq()
    {
        var owner = await factory.RegisterBuyerAsync();
        var intruder = await factory.RegisterBuyerAsync();
        var rfq = await owner.CreateRfqAsync();

        var response = await intruder.Client.GetAsync($"/api/rfqs/{rfq.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Buyer_CannotEditAnotherBuyersRfq()
    {
        var owner = await factory.RegisterBuyerAsync();
        var intruder = await factory.RegisterBuyerAsync();
        var rfq = await owner.CreateRfqAsync();

        var response = await intruder.Client.PutAsJsonAsync($"/api/rfqs/{rfq.Id}", new
        {
            productName = "Hijacked",
            description = "Changed by someone who does not own this RFQ.",
            quantity = 1,
            deliveryLocation = "Nowhere",
            deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5).ToString("yyyy-MM-dd")
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Buyer_CannotDeleteAnotherBuyersRfq()
    {
        var owner = await factory.RegisterBuyerAsync();
        var intruder = await factory.RegisterBuyerAsync();
        var rfq = await owner.CreateRfqAsync();

        var response = await intruder.Client.DeleteAsync($"/api/rfqs/{rfq.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // And the RFQ is still there.
        (await owner.Client.GetAsync($"/api/rfqs/{rfq.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Buyer_CannotCloseAnotherBuyersRfq()
    {
        var owner = await factory.RegisterBuyerAsync();
        var intruder = await factory.RegisterBuyerAsync();
        var rfq = await owner.CreateRfqAsync();

        var response = await intruder.Client.PostAsync($"/api/rfqs/{rfq.Id}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Buyer_CannotReadQuotationsOnAnotherBuyersRfq()
    {
        // Having the Buyer role is not enough: it has to be your own RFQ.
        var owner = await factory.RegisterBuyerAsync();
        var intruder = await factory.RegisterBuyerAsync();
        var rfq = await owner.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();
        await supplier.SubmitQuotationOrThrowAsync(rfq.Id);

        var response = await intruder.Client.GetAsync($"/api/rfqs/{rfq.Id}/quotations");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Supplier_OnlySeesTheirOwnQuotations()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();

        var mine = await factory.RegisterSupplierAsync();
        var rival = await factory.RegisterSupplierAsync();
        await mine.SubmitQuotationOrThrowAsync(rfq.Id, price: 100_000m);
        await rival.SubmitQuotationOrThrowAsync(rfq.Id, price: 200_000m);

        var response = await mine.Client.GetAsync("/api/quotations/my");
        var quotations = await response.ReadAsync<List<Api.DTOs.Quotations.MyQuotationDto>>();

        quotations.Should().HaveCount(1);
        quotations[0].QuotedPrice.Should().Be(100_000m);
    }

    [Fact]
    public async Task Buyer_RfqListNeverIncludesAnotherBuyersRfqs()
    {
        var owner = await factory.RegisterBuyerAsync();
        var other = await factory.RegisterBuyerAsync();
        await owner.CreateRfqAsync(productName: "Owner Only Widgets");

        var response = await other.Client.GetAsync("/api/rfqs/my");
        var page = await response.ReadAsync<Api.DTOs.Common.PagedResult<Api.DTOs.Rfqs.RfqListItemDto>>();

        page.Items.Should().NotContain(item => item.ProductName == "Owner Only Widgets");
    }

    [Fact]
    public async Task MissingRfq_Returns404NotForbidden()
    {
        var buyer = await factory.RegisterBuyerAsync();

        var response = await buyer.Client.GetAsync($"/api/rfqs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
