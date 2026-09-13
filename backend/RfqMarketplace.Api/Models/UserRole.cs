namespace RfqMarketplace.Api.Models;

/// <summary>
/// The two roles the marketplace supports. These strings are also the names of the
/// ASP.NET Core Identity roles, which are the single source of truth for authorization.
/// </summary>
public static class UserRole
{
    public const string Buyer = "Buyer";
    public const string Supplier = "Supplier";

    public static readonly string[] All = [Buyer, Supplier];

    public static bool IsValid(string? role) =>
        role is not null && All.Contains(role, StringComparer.Ordinal);
}
