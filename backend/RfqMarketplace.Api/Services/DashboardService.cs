using Microsoft.EntityFrameworkCore;
using RfqMarketplace.Api.Data;
using RfqMarketplace.Api.DTOs.Common;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Services;

public interface IDashboardService
{
    Task<BuyerDashboardDto> GetBuyerSummaryAsync(Guid buyerId, CancellationToken ct = default);
    Task<SupplierDashboardDto> GetSupplierSummaryAsync(Guid supplierId, CancellationToken ct = default);
}

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<BuyerDashboardDto> GetBuyerSummaryAsync(Guid buyerId, CancellationToken ct = default)
    {
        var counts = await db.Rfqs.AsNoTracking()
            .Where(r => r.BuyerId == buyerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Open = g.Count(r => r.Status == RfqStatus.Open),
                Quotations = g.Sum(r => r.Quotations.Count)
            })
            .FirstOrDefaultAsync(ct);

        return counts is null
            ? new BuyerDashboardDto(0, 0, 0, 0)
            : new BuyerDashboardDto(counts.Total, counts.Open, counts.Total - counts.Open, counts.Quotations);
    }

    public async Task<SupplierDashboardDto> GetSupplierSummaryAsync(Guid supplierId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var available = db.Rfqs.AsNoTracking()
            .Where(r => r.Status == RfqStatus.Open && r.Deadline >= today);

        var availableCount = await available.CountAsync(ct);
        var notQuotedCount = await available.CountAsync(r => r.Quotations.All(q => q.SupplierId != supplierId), ct);
        var submittedCount = await db.Quotations.AsNoTracking().CountAsync(q => q.SupplierId == supplierId, ct);

        return new SupplierDashboardDto(availableCount, submittedCount, notQuotedCount);
    }
}
