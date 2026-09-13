namespace RfqMarketplace.Api.DTOs.Quotations;

public record CreateQuotationRequest(
    decimal QuotedPrice,
    int EstimatedDeliveryDays,
    string? Message);

/// <summary>A quotation as the RFQ owner sees it, including who sent it.</summary>
public record QuotationDto(
    Guid Id,
    Guid RfqId,
    Guid SupplierId,
    string SupplierName,
    string SupplierCompanyName,
    decimal QuotedPrice,
    int EstimatedDeliveryDays,
    string? Message,
    DateTimeOffset CreatedAt);

/// <summary>A quotation as its author sees it, with enough RFQ context to be useful on its own.</summary>
public record MyQuotationDto(
    Guid Id,
    Guid RfqId,
    string RfqProductName,
    string RfqDeliveryLocation,
    DateOnly RfqDeadline,
    string RfqStatus,
    string BuyerCompanyName,
    decimal QuotedPrice,
    int EstimatedDeliveryDays,
    string? Message,
    DateTimeOffset CreatedAt);
