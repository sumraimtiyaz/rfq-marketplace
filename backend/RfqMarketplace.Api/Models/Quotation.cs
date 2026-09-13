namespace RfqMarketplace.Api.Models;

/// <summary>
/// A supplier's response to an RFQ. Joins the RFQ to the supplier who priced it;
/// a unique index on (RfqId, SupplierId) enforces one quotation per supplier per RFQ.
/// </summary>
public class Quotation
{
    public Guid Id { get; set; }

    public Guid RfqId { get; set; }
    public Rfq Rfq { get; set; } = null!;

    public Guid SupplierId { get; set; }
    public ApplicationUser Supplier { get; set; } = null!;

    public decimal QuotedPrice { get; set; }

    /// <summary>Lead time in days. Stored as a number so it can be validated and compared.</summary>
    public int EstimatedDeliveryDays { get; set; }

    public string? Message { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
