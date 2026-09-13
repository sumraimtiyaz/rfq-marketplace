using System.Security.Claims;
using RfqMarketplace.Api.Common;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Services;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid Id
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new AuthenticationFailedException("You need to be logged in to do that.");
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool IsBuyer => string.Equals(Role, UserRole.Buyer, StringComparison.Ordinal);
    public bool IsSupplier => string.Equals(Role, UserRole.Supplier, StringComparison.Ordinal);
}
