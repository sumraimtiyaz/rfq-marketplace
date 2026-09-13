namespace RfqMarketplace.Api.Services;

/// <summary>
/// The authenticated caller, read from the validated token's claims. Services depend on
/// this rather than on HttpContext so they stay testable and transport-agnostic.
/// </summary>
public interface ICurrentUser
{
    Guid Id { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsBuyer { get; }
    bool IsSupplier { get; }
}
