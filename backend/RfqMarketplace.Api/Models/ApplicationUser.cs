using Microsoft.AspNetCore.Identity;

namespace RfqMarketplace.Api.Models;

/// <summary>
/// Identity user for both buyers and suppliers. The role itself lives in the Identity
/// role tables (AspNetRoles / AspNetUserRoles) rather than on a column here, so there is
/// exactly one place that decides what a user is allowed to do.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Contact person's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Business the user represents. Shown to the other side of the marketplace.</summary>
    public string CompanyName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Rfq> Rfqs { get; set; } = new List<Rfq>();
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
}
