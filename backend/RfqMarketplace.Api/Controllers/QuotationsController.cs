using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfqMarketplace.Api.DTOs.Quotations;
using RfqMarketplace.Api.Models;
using RfqMarketplace.Api.Services;

namespace RfqMarketplace.Api.Controllers;

[ApiController]
[Route("api/quotations")]
[Authorize]
[Produces("application/json")]
public class QuotationsController(IQuotationService quotationService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Supplier: every quotation they have submitted. Scoped to the caller's own id, so this
    /// endpoint can never return a competitor's pricing.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = UserRole.Supplier)]
    [ProducesResponseType(typeof(IReadOnlyList<MyQuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MyQuotationDto>>> Mine(CancellationToken ct)
        => Ok(await quotationService.GetMineAsync(currentUser.Id, ct));
}
