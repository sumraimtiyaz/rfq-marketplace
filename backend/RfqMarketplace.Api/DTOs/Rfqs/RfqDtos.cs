using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.DTOs.Rfqs;

/// <summary>
/// The writable shape of an RFQ. Create and update carry the same fields, so they share one
/// contract and one validator while staying distinct types in the API surface.
/// </summary>
public interface IRfqWriteRequest
{
    string ProductName { get; }
    string Description { get; }
    int Quantity { get; }
    string DeliveryLocation { get; }
    DateOnly Deadline { get; }
}

public record CreateRfqRequest(
    string ProductName,
    string Description,
    int Quantity,
    string DeliveryLocation,
    DateOnly Deadline) : IRfqWriteRequest;

public record UpdateRfqRequest(
    string ProductName,
    string Description,
    int Quantity,
    string DeliveryLocation,
    DateOnly Deadline) : IRfqWriteRequest;

/// <summary>Summary row used in list views on both sides of the marketplace.</summary>
public record RfqListItemDto(
    Guid Id,
    string ProductName,
    int Quantity,
    string DeliveryLocation,
    DateOnly Deadline,
    RfqStatus Status,
    bool IsExpired,
    string BuyerCompanyName,
    int QuotationCount,
    bool HasQuoted,
    DateTimeOffset CreatedAt);

/// <summary>
/// Full RFQ detail. <c>QuotationCount</c> is only populated for the buyer who owns the RFQ;
/// suppliers get <c>HasQuoted</c> instead and never learn how many rivals responded.
/// </summary>
public record RfqDetailDto(
    Guid Id,
    string ProductName,
    string Description,
    int Quantity,
    string DeliveryLocation,
    DateOnly Deadline,
    RfqStatus Status,
    bool IsExpired,
    bool AcceptsQuotations,
    Guid BuyerId,
    string BuyerCompanyName,
    bool IsOwner,
    int? QuotationCount,
    bool HasQuoted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Query string options for browsing and searching RFQs.</summary>
public class RfqQueryParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    private int _page = 1;

    /// <summary>Free-text match against product name and description.</summary>
    public string? Search { get; set; }

    public string? Location { get; set; }

    public RfqStatus? Status { get; set; }

    /// <summary>Suppliers see actionable RFQs by default; set true to include past-deadline ones.</summary>
    public bool IncludeExpired { get; set; }

    /// <summary>createdAt | deadline | quantity, optionally suffixed with _asc or _desc.</summary>
    public string? SortBy { get; set; }

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch { < 1 => 10, > MaxPageSize => MaxPageSize, _ => value };
    }
}
