namespace RfqMarketplace.Api.DTOs.Common;

public record BuyerDashboardDto(
    int TotalRfqs,
    int OpenRfqs,
    int ClosedRfqs,
    int QuotationsReceived);

public record SupplierDashboardDto(
    int AvailableRfqs,
    int QuotationsSubmitted,
    int OpenRfqsNotQuoted);
