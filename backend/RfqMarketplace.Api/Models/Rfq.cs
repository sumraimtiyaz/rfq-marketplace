namespace RfqMarketplace.Api.Models;

/// <summary>
/// A buyer's request for quotation. <see cref="BuyerId"/> is what makes ownership checks
/// possible: every mutation and every quotation read is gated on it.
/// </summary>
public class Rfq
{
    public Guid Id { get; set; }

    public Guid BuyerId { get; set; }
    public ApplicationUser Buyer { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string DeliveryLocation { get; set; } = string.Empty;
    public DateOnly Deadline { get; set; }
    public RfqStatus Status { get; set; } = RfqStatus.Open;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    /// <summary>True once the deadline day has passed (UTC). Expired RFQs stop accepting quotations.</summary>
    public bool IsExpired(DateOnly today) => Deadline < today;

    /// <summary>An RFQ only accepts quotations while it is open and inside its deadline.</summary>
    public bool AcceptsQuotations(DateOnly today) => Status == RfqStatus.Open && !IsExpired(today);
}
