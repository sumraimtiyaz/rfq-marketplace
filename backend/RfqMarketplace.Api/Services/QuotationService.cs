using Microsoft.EntityFrameworkCore;
using RfqMarketplace.Api.Common;
using RfqMarketplace.Api.Data;
using RfqMarketplace.Api.DTOs.Quotations;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Services;

public interface IQuotationService
{
    Task<QuotationDto> SubmitAsync(Guid rfqId, Guid supplierId, CreateQuotationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QuotationDto>> GetForRfqAsync(Guid rfqId, Guid buyerId, CancellationToken ct = default);
    Task<IReadOnlyList<MyQuotationDto>> GetMineAsync(Guid supplierId, CancellationToken ct = default);
}

public class QuotationService(AppDbContext db, ILogger<QuotationService> logger) : IQuotationService
{
    public async Task<QuotationDto> SubmitAsync(
        Guid rfqId, Guid supplierId, CreateQuotationRequest request, CancellationToken ct = default)
    {
        var rfq = await db.Rfqs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rfqId, ct)
            ?? throw new NotFoundException("RFQ not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (rfq.Status != RfqStatus.Open)
            throw new BusinessRuleException("This RFQ is closed and is no longer accepting quotations.");

        if (rfq.IsExpired(today))
            throw new BusinessRuleException("The deadline for this RFQ has passed.");

        // Cheap pre-check for a friendly message. It is not the guarantee - the unique index is,
        // which is what makes two simultaneous submissions safe.
        if (await db.Quotations.AnyAsync(q => q.RfqId == rfqId && q.SupplierId == supplierId, ct))
            throw new ConflictException("You have already submitted a quotation for this RFQ.");

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            RfqId = rfqId,
            SupplierId = supplierId,
            QuotedPrice = request.QuotedPrice,
            EstimatedDeliveryDays = request.EstimatedDeliveryDays,
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Quotations.Add(quotation);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another request for the same (RFQ, supplier) pair committed between the check above
            // and this insert. The database rejected it, so report the same 409 as the pre-check.
            logger.LogInformation(ex, "Duplicate quotation race for RFQ {RfqId} by supplier {SupplierId}", rfqId, supplierId);
            throw new ConflictException("You have already submitted a quotation for this RFQ.");
        }

        logger.LogInformation("Supplier {SupplierId} quoted {QuotationId} on RFQ {RfqId}", supplierId, quotation.Id, rfqId);

        var supplier = await db.Users.AsNoTracking().FirstAsync(u => u.Id == supplierId, ct);
        return ToDto(quotation, supplier);
    }

    public async Task<IReadOnlyList<QuotationDto>> GetForRfqAsync(
        Guid rfqId, Guid buyerId, CancellationToken ct = default)
    {
        // Only the buyer who owns the RFQ may read the quotations on it. Holding the Buyer role
        // is not sufficient - this is the resource-level check.
        var ownerId = await db.Rfqs.AsNoTracking()
            .Where(r => r.Id == rfqId)
            .Select(r => (Guid?)r.BuyerId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("RFQ not found.");

        if (ownerId != buyerId)
            throw new ForbiddenException("You don't have permission to view quotations for this RFQ.");

        return await db.Quotations.AsNoTracking()
            .Where(q => q.RfqId == rfqId)
            .OrderBy(q => q.QuotedPrice)
            .Select(q => new QuotationDto(
                q.Id,
                q.RfqId,
                q.SupplierId,
                q.Supplier.Name,
                q.Supplier.CompanyName,
                q.QuotedPrice,
                q.EstimatedDeliveryDays,
                q.Message,
                q.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MyQuotationDto>> GetMineAsync(Guid supplierId, CancellationToken ct = default) =>
        await db.Quotations.AsNoTracking()
            .Where(q => q.SupplierId == supplierId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new MyQuotationDto(
                q.Id,
                q.RfqId,
                q.Rfq.ProductName,
                q.Rfq.DeliveryLocation,
                q.Rfq.Deadline,
                q.Rfq.Status.ToString(),
                q.Rfq.Buyer.CompanyName,
                q.QuotedPrice,
                q.EstimatedDeliveryDays,
                q.Message,
                q.CreatedAt))
            .ToListAsync(ct);

    /// <summary>PostgreSQL reports unique violations as SQLSTATE 23505; SQLite uses code 19 (constraint).</summary>
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.GetType().GetProperty("SqlState")?.GetValue(ex.InnerException) as string == "23505"
        || ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;

    private static QuotationDto ToDto(Quotation q, ApplicationUser supplier) => new(
        q.Id, q.RfqId, q.SupplierId, supplier.Name, supplier.CompanyName,
        q.QuotedPrice, q.EstimatedDeliveryDays, q.Message, q.CreatedAt);
}
