using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfqMarketplace.Api.DTOs.Common;
using RfqMarketplace.Api.Models;
using RfqMarketplace.Api.Services;

namespace RfqMarketplace.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController(IDashboardService dashboardService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Counters for the buyer landing page.</summary>
    [HttpGet("buyer")]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(typeof(BuyerDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BuyerDashboardDto>> Buyer(CancellationToken ct)
        => Ok(await dashboardService.GetBuyerSummaryAsync(currentUser.Id, ct));

    /// <summary>Counters for the supplier landing page.</summary>
    [HttpGet("supplier")]
    [Authorize(Roles = UserRole.Supplier)]
    [ProducesResponseType(typeof(SupplierDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDashboardDto>> Supplier(CancellationToken ct)
        => Ok(await dashboardService.GetSupplierSummaryAsync(currentUser.Id, ct));
}
