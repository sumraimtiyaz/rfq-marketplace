using Microsoft.EntityFrameworkCore;
using RfqMarketplace.Api.Common;
using RfqMarketplace.Api.Data;
using RfqMarketplace.Api.DTOs.Common;
using RfqMarketplace.Api.DTOs.Rfqs;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Services;

public interface IRfqService
{
    Task<RfqDetailDto> CreateAsync(Guid buyerId, CreateRfqRequest request, CancellationToken ct = default);
    Task<PagedResult<RfqListItemDto>> GetMyRfqsAsync(Guid buyerId, RfqQueryParameters query, CancellationToken ct = default);
    Task<PagedResult<RfqListItemDto>> BrowseAsync(Guid supplierId, RfqQueryParameters query, CancellationToken ct = default);
    Task<RfqDetailDto> GetByIdAsync(Guid rfqId, CancellationToken ct = default);
    Task<RfqDetailDto> UpdateAsync(Guid rfqId, Guid buyerId, UpdateRfqRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid rfqId, Guid buyerId, CancellationToken ct = default);
    Task<RfqDetailDto> SetStatusAsync(Guid rfqId, Guid buyerId, RfqStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDeliveryLocationsAsync(CancellationToken ct = default);
}

public class RfqService(AppDbContext db, ICurrentUser currentUser, ILogger<RfqService> logger) : IRfqService
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<RfqDetailDto> CreateAsync(Guid buyerId, CreateRfqRequest request, CancellationToken ct = default)
    {
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            ProductName = request.ProductName.Trim(),
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            DeliveryLocation = request.DeliveryLocation.Trim(),
            Deadline = request.Deadline,
            Status = RfqStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Rfqs.Add(rfq);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Buyer {BuyerId} created RFQ {RfqId}", buyerId, rfq.Id);

        return await GetByIdAsync(rfq.Id, ct);
    }

    public async Task<PagedResult<RfqListItemDto>> GetMyRfqsAsync(
        Guid buyerId, RfqQueryParameters query, CancellationToken ct = default)
    {
        // Scoped to the caller's own RFQs before any filter runs, so a buyer can never
        // page their way into another buyer's requirements.
        var source = db.Rfqs.AsNoTracking().Where(r => r.BuyerId == buyerId);
        return await ProjectPagedAsync(source, query, supplierId: null, includeQuotationCount: true, ct);
    }

    public async Task<PagedResult<RfqListItemDto>> BrowseAsync(
        Guid supplierId, RfqQueryParameters query, CancellationToken ct = default)
    {
        // Suppliers only ever see open requirements; closed RFQs leave the marketplace.
        var source = db.Rfqs.AsNoTracking().Where(r => r.Status == RfqStatus.Open);

        if (!query.IncludeExpired)
        {
            var today = Today;
            source = source.Where(r => r.Deadline >= today);
        }

        return await ProjectPagedAsync(source, query, supplierId, includeQuotationCount: false, ct);
    }

    public async Task<RfqDetailDto> GetByIdAsync(Guid rfqId, CancellationToken ct = default)
    {
        var callerId = currentUser.Id;
        var isBuyer = currentUser.IsBuyer;

        var row = await db.Rfqs.AsNoTracking()
            .Where(r => r.Id == rfqId)
            .Select(r => new
            {
                Rfq = r,
                BuyerCompanyName = r.Buyer.CompanyName,
                QuotationCount = r.Quotations.Count,
                HasQuoted = r.Quotations.Any(q => q.SupplierId == callerId)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("RFQ not found.");

        var isOwner = row.Rfq.BuyerId == callerId;

        // Resource-level authorization: holding the Buyer role is not enough, it has to be
        // *your* RFQ. Suppliers may read any RFQ because publishing them is the point.
        if (isBuyer && !isOwner)
            throw new ForbiddenException("You don't have permission to access this RFQ.");

        return ToDetail(row.Rfq, row.BuyerCompanyName, isOwner,
            isOwner ? row.QuotationCount : null, row.HasQuoted);
    }

    public async Task<RfqDetailDto> UpdateAsync(
        Guid rfqId, Guid buyerId, UpdateRfqRequest request, CancellationToken ct = default)
    {
        var rfq = await LoadOwnedAsync(rfqId, buyerId, ct);

        if (rfq.Status != RfqStatus.Open)
            throw new BusinessRuleException("A closed RFQ cannot be edited. Reopen it first.");

        rfq.ProductName = request.ProductName.Trim();
        rfq.Description = request.Description.Trim();
        rfq.Quantity = request.Quantity;
        rfq.DeliveryLocation = request.DeliveryLocation.Trim();
        rfq.Deadline = request.Deadline;
        rfq.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(rfqId, ct);
    }

    public async Task DeleteAsync(Guid rfqId, Guid buyerId, CancellationToken ct = default)
    {
        var rfq = await LoadOwnedAsync(rfqId, buyerId, ct);

        // Quotations are cascade-deleted along with the RFQ they belong to.
        db.Rfqs.Remove(rfq);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Buyer {BuyerId} deleted RFQ {RfqId}", buyerId, rfqId);
    }

    public async Task<RfqDetailDto> SetStatusAsync(
        Guid rfqId, Guid buyerId, RfqStatus status, CancellationToken ct = default)
    {
        var rfq = await LoadOwnedAsync(rfqId, buyerId, ct);

        if (rfq.Status == status)
            throw new BusinessRuleException($"This RFQ is already {status.ToString().ToLowerInvariant()}.");

        rfq.Status = status;
        rfq.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(rfqId, ct);
    }

    public async Task<IReadOnlyList<string>> GetDeliveryLocationsAsync(CancellationToken ct = default) =>
        await db.Rfqs.AsNoTracking()
            .Where(r => r.Status == RfqStatus.Open)
            .Select(r => r.DeliveryLocation)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync(ct);

    /// <summary>Loads an RFQ and asserts the caller owns it: 404 when missing, 403 when someone else's.</summary>
    private async Task<Rfq> LoadOwnedAsync(Guid rfqId, Guid buyerId, CancellationToken ct)
    {
        var rfq = await db.Rfqs.FirstOrDefaultAsync(r => r.Id == rfqId, ct)
            ?? throw new NotFoundException("RFQ not found.");

        if (rfq.BuyerId != buyerId)
            throw new ForbiddenException("You don't have permission to modify this RFQ.");

        return rfq;
    }

    private static async Task<PagedResult<RfqListItemDto>> ProjectPagedAsync(
        IQueryable<Rfq> source,
        RfqQueryParameters query,
        Guid? supplierId,
        bool includeQuotationCount,
        CancellationToken ct)
    {
        // Lower-casing both sides keeps the search case-insensitive on PostgreSQL (where LIKE
        // is case-sensitive) without reaching for a provider-specific operator such as ILIKE.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim().ToLower()}%";
            source = source.Where(r =>
                EF.Functions.Like(r.ProductName.ToLower(), term) ||
                EF.Functions.Like(r.Description.ToLower(), term));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = $"%{query.Location.Trim().ToLower()}%";
            source = source.Where(r => EF.Functions.Like(r.DeliveryLocation.ToLower(), location));
        }

        if (query.Status.HasValue)
            source = source.Where(r => r.Status == query.Status.Value);

        source = ApplySort(source, query.SortBy);

        var totalCount = await source.CountAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new RfqListItemDto(
                r.Id,
                r.ProductName,
                r.Quantity,
                r.DeliveryLocation,
                r.Deadline,
                r.Status,
                r.Deadline < today,
                r.Buyer.CompanyName,
                includeQuotationCount ? r.Quotations.Count : 0,
                supplierId != null && r.Quotations.Any(q => q.SupplierId == supplierId),
                r.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<RfqListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    private static IQueryable<Rfq> ApplySort(IQueryable<Rfq> source, string? sortBy)
    {
        var (field, descending) = ParseSort(sortBy);

        return (field, descending) switch
        {
            ("deadline", true) => source.OrderByDescending(r => r.Deadline).ThenByDescending(r => r.CreatedAt),
            ("deadline", false) => source.OrderBy(r => r.Deadline).ThenByDescending(r => r.CreatedAt),
            ("quantity", true) => source.OrderByDescending(r => r.Quantity).ThenByDescending(r => r.CreatedAt),
            ("quantity", false) => source.OrderBy(r => r.Quantity).ThenByDescending(r => r.CreatedAt),
            (_, false) => source.OrderBy(r => r.CreatedAt),
            _ => source.OrderByDescending(r => r.CreatedAt)
        };
    }

    /// <summary>Accepts "deadline", "quantity", "createdAt", each optionally suffixed _asc / _desc.</summary>
    private static (string Field, bool Descending) ParseSort(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return ("createdat", true);

        var value = sortBy.Trim().ToLowerInvariant();

        if (value.EndsWith("_asc", StringComparison.Ordinal))
            return (value[..^4], false);

        if (value.EndsWith("_desc", StringComparison.Ordinal))
            return (value[..^5], true);

        // Newest-first is the sensible default for a feed; deadlines and quantities read better ascending.
        return (value, value is "createdat");
    }

    private static RfqDetailDto ToDetail(
        Rfq rfq, string buyerCompanyName, bool isOwner, int? quotationCount, bool hasQuoted)
    {
        var today = Today;
        return new RfqDetailDto(
            rfq.Id,
            rfq.ProductName,
            rfq.Description,
            rfq.Quantity,
            rfq.DeliveryLocation,
            rfq.Deadline,
            rfq.Status,
            rfq.IsExpired(today),
            rfq.AcceptsQuotations(today),
            rfq.BuyerId,
            buyerCompanyName,
            isOwner,
            quotationCount,
            hasQuoted,
            rfq.CreatedAt,
            rfq.UpdatedAt);
    }
}
