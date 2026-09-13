using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RfqMarketplace.Api.DTOs.Common;
using RfqMarketplace.Api.DTOs.Quotations;
using RfqMarketplace.Api.DTOs.Rfqs;
using RfqMarketplace.Api.Models;
using RfqMarketplace.Api.Tests.Infrastructure;

namespace RfqMarketplace.Api.Tests;

/// <summary>The core loop the whole product exists for, plus the browse/search that feeds it.</summary>
public class MarketplaceFlowTests(RfqApiFactory factory) : IClassFixture<RfqApiFactory>
{
    [Fact]
    public async Task BuyerPostsRequirement_SupplierFindsAndQuotes_BuyerSeesTheQuotation()
    {
        // 1. A buyer publishes a requirement.
        var buyer = await factory.RegisterBuyerAsync(company: "Meridian Office Solutions");
        var rfq = await buyer.CreateRfqAsync(
            productName: "Ergonomic Office Chairs",
            quantity: 500,
            location: "Ahmedabad");

        // 2. A supplier discovers it by searching.
        var supplier = await factory.RegisterSupplierAsync(company: "ABC Furniture Works");
        var browse = await supplier.Client.GetAsync("/api/rfqs?search=ergonomic");
        var results = await browse.ReadAsync<PagedResult<RfqListItemDto>>();

        results.Items.Should().Contain(item => item.Id == rfq.Id);
        results.Items.Single(item => item.Id == rfq.Id).BuyerCompanyName
            .Should().Be("Meridian Office Solutions");

        // 3. The supplier opens the full requirement.
        var detail = await (await supplier.Client.GetAsync($"/api/rfqs/{rfq.Id}"))
            .ReadAsync<RfqDetailDto>();

        detail.Quantity.Should().Be(500);
        detail.AcceptsQuotations.Should().BeTrue();
        detail.HasQuoted.Should().BeFalse();
        detail.QuotationCount.Should().BeNull("suppliers must not learn how many rivals responded");

        // 4. The supplier quotes.
        await supplier.SubmitQuotationOrThrowAsync(rfq.Id, price: 1_250_000m, days: 20);

        // 5. The buyer sees it.
        var quotations = await (await buyer.Client.GetAsync($"/api/rfqs/{rfq.Id}/quotations"))
            .ReadAsync<List<QuotationDto>>();

        quotations.Should().ContainSingle();
        quotations[0].SupplierCompanyName.Should().Be("ABC Furniture Works");
        quotations[0].QuotedPrice.Should().Be(1_250_000m);
        quotations[0].EstimatedDeliveryDays.Should().Be(20);

        // 6. And the supplier can review what they sent.
        var mine = await (await supplier.Client.GetAsync("/api/quotations/my"))
            .ReadAsync<List<MyQuotationDto>>();

        mine.Should().ContainSingle(q => q.RfqId == rfq.Id);
        mine[0].RfqProductName.Should().Be("Ergonomic Office Chairs");
        mine[0].BuyerCompanyName.Should().Be("Meridian Office Solutions");
    }

    [Fact]
    public async Task Search_MatchesProductNameAndDescription_CaseInsensitively()
    {
        var buyer = await factory.RegisterBuyerAsync();
        await buyer.CreateRfqAsync(productName: "Titanium Fasteners", description: "Aerospace grade bolts.");
        await buyer.CreateRfqAsync(productName: "Cardboard Cartons", description: "Includes titanium-free liners.");
        await buyer.CreateRfqAsync(productName: "Rubber Gaskets", description: "Nitrile rubber, 3mm.");

        var supplier = await factory.RegisterSupplierAsync();
        var results = await (await supplier.Client.GetAsync("/api/rfqs?search=TITANIUM"))
            .ReadAsync<PagedResult<RfqListItemDto>>();

        results.Items.Should().HaveCount(2);
        results.Items.Should().NotContain(item => item.ProductName == "Rubber Gaskets");
    }

    [Fact]
    public async Task LocationFilter_NarrowsResults()
    {
        var buyer = await factory.RegisterBuyerAsync();
        await buyer.CreateRfqAsync(productName: "Filter Test Alpha", location: "Vadodara, Gujarat");
        await buyer.CreateRfqAsync(productName: "Filter Test Beta", location: "Ahmedabad, Gujarat");

        var supplier = await factory.RegisterSupplierAsync();
        var results = await (await supplier.Client.GetAsync("/api/rfqs?search=Filter Test&location=vadodara"))
            .ReadAsync<PagedResult<RfqListItemDto>>();

        results.Items.Should().ContainSingle();
        results.Items[0].ProductName.Should().Be("Filter Test Alpha");
    }

    [Fact]
    public async Task Browse_ExcludesClosedRfqs()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync(productName: "Soon To Be Closed");
        await buyer.Client.PostAsync($"/api/rfqs/{rfq.Id}/close", null);

        var supplier = await factory.RegisterSupplierAsync();
        var results = await (await supplier.Client.GetAsync("/api/rfqs?search=Soon To Be Closed"))
            .ReadAsync<PagedResult<RfqListItemDto>>();

        results.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Browse_ExcludesExpiredRfqsUnlessAskedFor()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync(productName: "Expired Widget Order");

        await factory.WithDbAsync(async db =>
        {
            var stored = db.Rfqs.Single(r => r.Id == rfq.Id);
            stored.Deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
            await db.SaveChangesAsync();
        });

        var supplier = await factory.RegisterSupplierAsync();

        var defaultResults = await (await supplier.Client.GetAsync("/api/rfqs?search=Expired Widget"))
            .ReadAsync<PagedResult<RfqListItemDto>>();
        defaultResults.Items.Should().BeEmpty();

        var withExpired = await (await supplier.Client.GetAsync("/api/rfqs?search=Expired Widget&includeExpired=true"))
            .ReadAsync<PagedResult<RfqListItemDto>>();
        withExpired.Items.Should().ContainSingle();
        withExpired.Items[0].IsExpired.Should().BeTrue();
    }

    [Fact]
    public async Task Browse_TellsSupplierWhichRfqsTheyAlreadyQuoted()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var quoted = await buyer.CreateRfqAsync(productName: "HasQuoted Marker One");
        await buyer.CreateRfqAsync(productName: "HasQuoted Marker Two");

        var supplier = await factory.RegisterSupplierAsync();
        await supplier.SubmitQuotationOrThrowAsync(quoted.Id);

        var results = await (await supplier.Client.GetAsync("/api/rfqs?search=HasQuoted Marker"))
            .ReadAsync<PagedResult<RfqListItemDto>>();

        results.Items.Single(i => i.Id == quoted.Id).HasQuoted.Should().BeTrue();
        results.Items.Single(i => i.Id != quoted.Id).HasQuoted.Should().BeFalse();
    }

    [Fact]
    public async Task Paging_ReturnsRequestedSliceAndTotals()
    {
        var buyer = await factory.RegisterBuyerAsync();
        for (var i = 0; i < 7; i++)
            await buyer.CreateRfqAsync(productName: $"Paging Probe {i:D2}");

        var page = await (await buyer.Client.GetAsync("/api/rfqs/my?search=Paging Probe&page=2&pageSize=3"))
            .ReadAsync<PagedResult<RfqListItemDto>>();

        page.Items.Should().HaveCount(3);
        page.Page.Should().Be(2);
        page.TotalCount.Should().Be(7);
        page.TotalPages.Should().Be(3);
        page.HasNextPage.Should().BeTrue();
        page.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task PageSize_IsCappedToProtectTheDatabase()
    {
        var buyer = await factory.RegisterBuyerAsync();
        await buyer.CreateRfqAsync();

        var page = await (await buyer.Client.GetAsync("/api/rfqs/my?pageSize=100000"))
            .ReadAsync<PagedResult<RfqListItemDto>>();

        page.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task BuyerRfqList_ShowsQuotationCounts()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync(productName: "Counted Requirement");

        for (var i = 0; i < 2; i++)
        {
            var supplier = await factory.RegisterSupplierAsync();
            await supplier.SubmitQuotationOrThrowAsync(rfq.Id, price: 1000m + i);
        }

        var page = await (await buyer.Client.GetAsync("/api/rfqs/my?search=Counted Requirement"))
            .ReadAsync<PagedResult<RfqListItemDto>>();

        page.Items.Single().QuotationCount.Should().Be(2);
    }

    [Fact]
    public async Task Buyer_CanEditOpenRfq_ButNotClosedOne()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();

        object update = new
        {
            productName = "Revised Chairs",
            description = "Now with headrests.",
            quantity = 600,
            deliveryLocation = "Surat",
            deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30).ToString("yyyy-MM-dd")
        };

        var edited = await (await buyer.Client.PutAsJsonAsync($"/api/rfqs/{rfq.Id}", update))
            .ReadAsync<RfqDetailDto>();
        edited.ProductName.Should().Be("Revised Chairs");
        edited.Quantity.Should().Be(600);

        await buyer.Client.PostAsync($"/api/rfqs/{rfq.Id}/close", null);
        var afterClose = await buyer.Client.PutAsJsonAsync($"/api/rfqs/{rfq.Id}", update);

        afterClose.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Buyer_CanCloseAndReopenTheirRfq()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();

        var closed = await (await buyer.Client.PostAsync($"/api/rfqs/{rfq.Id}/close", null))
            .ReadAsync<RfqDetailDto>();
        closed.Status.Should().Be(RfqStatus.Closed);
        closed.AcceptsQuotations.Should().BeFalse();

        // Closing twice is a no-op the API refuses rather than silently accepting.
        (await buyer.Client.PostAsync($"/api/rfqs/{rfq.Id}/close", null)).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);

        var reopened = await (await buyer.Client.PostAsync($"/api/rfqs/{rfq.Id}/reopen", null))
            .ReadAsync<RfqDetailDto>();
        reopened.Status.Should().Be(RfqStatus.Open);
    }

    [Fact]
    public async Task BuyerDashboard_CountsRfqsAndQuotations()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var open = await buyer.CreateRfqAsync();
        var toClose = await buyer.CreateRfqAsync();
        await buyer.Client.PostAsync($"/api/rfqs/{toClose.Id}/close", null);

        var supplier = await factory.RegisterSupplierAsync();
        await supplier.SubmitQuotationOrThrowAsync(open.Id);

        var summary = await (await buyer.Client.GetAsync("/api/dashboard/buyer"))
            .ReadAsync<BuyerDashboardDto>();

        summary.TotalRfqs.Should().Be(2);
        summary.OpenRfqs.Should().Be(1);
        summary.ClosedRfqs.Should().Be(1);
        summary.QuotationsReceived.Should().Be(1);
    }

    [Fact]
    public async Task SupplierDashboard_CountsSubmittedQuotations()
    {
        var buyer = await factory.RegisterBuyerAsync();
        var rfq = await buyer.CreateRfqAsync();
        var supplier = await factory.RegisterSupplierAsync();
        await supplier.SubmitQuotationOrThrowAsync(rfq.Id);

        var summary = await (await supplier.Client.GetAsync("/api/dashboard/supplier"))
            .ReadAsync<SupplierDashboardDto>();

        summary.QuotationsSubmitted.Should().Be(1);
        summary.AvailableRfqs.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task LocationsEndpoint_ReturnsDistinctSortedLocations()
    {
        var buyer = await factory.RegisterBuyerAsync();
        await buyer.CreateRfqAsync(location: "Zeta City");
        await buyer.CreateRfqAsync(location: "Zeta City");
        await buyer.CreateRfqAsync(location: "Alpha City");

        var supplier = await factory.RegisterSupplierAsync();
        var locations = await (await supplier.Client.GetAsync("/api/rfqs/locations"))
            .ReadAsync<List<string>>();

        locations.Should().OnlyHaveUniqueItems();
        locations.Should().Contain(["Alpha City", "Zeta City"]);
        locations.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Health_ReportsDatabaseReachable()
    {
        var response = await factory.CreateClient().GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
