using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfqMarketplace.Api.DTOs.Common;
using RfqMarketplace.Api.DTOs.Quotations;
using RfqMarketplace.Api.DTOs.Rfqs;
using RfqMarketplace.Api.Models;
using RfqMarketplace.Api.Services;

namespace RfqMarketplace.Api.Controllers;

/// <summary>
/// Every endpoint here is gated twice: <c>[Authorize(Roles = ...)]</c> decides who may call it at
/// all, and the service layer then checks that the RFQ actually belongs to the caller. Hiding a
/// button in the UI is not authorization.
/// </summary>
[ApiController]
[Route("api/rfqs")]
[Authorize]
[Produces("application/json")]
public class RfqsController(
    IRfqService rfqService,
    IQuotationService quotationService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Buyer: publish a new requirement.</summary>
    [HttpPost]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(typeof(RfqDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RfqDetailDto>> Create(CreateRfqRequest request, CancellationToken ct)
    {
        var rfq = await rfqService.CreateAsync(currentUser.Id, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = rfq.Id }, rfq);
    }

    /// <summary>Supplier: browse open RFQs, with search, location filter, sorting and paging.</summary>
    [HttpGet]
    [Authorize(Roles = UserRole.Supplier)]
    [ProducesResponseType(typeof(PagedResult<RfqListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RfqListItemDto>>> Browse(
        [FromQuery] RfqQueryParameters query, CancellationToken ct)
        => Ok(await rfqService.BrowseAsync(currentUser.Id, query, ct));

    /// <summary>Buyer: list the RFQs they created.</summary>
    [HttpGet("my")]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(typeof(PagedResult<RfqListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RfqListItemDto>>> Mine(
        [FromQuery] RfqQueryParameters query, CancellationToken ct)
        => Ok(await rfqService.GetMyRfqsAsync(currentUser.Id, query, ct));

    /// <summary>Distinct delivery locations across open RFQs, for the supplier filter dropdown.</summary>
    [HttpGet("locations")]
    [Authorize(Roles = UserRole.Supplier)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> Locations(CancellationToken ct)
        => Ok(await rfqService.GetDeliveryLocationsAsync(ct));

    /// <summary>Full RFQ detail. Suppliers may read any RFQ; a buyer may only read their own.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RfqDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await rfqService.GetByIdAsync(id, ct));

    /// <summary>Buyer: edit an RFQ they own, while it is still open.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(typeof(RfqDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqDetailDto>> Update(Guid id, UpdateRfqRequest request, CancellationToken ct)
        => Ok(await rfqService.UpdateAsync(id, currentUser.Id, request, ct));

    /// <summary>Buyer: delete an RFQ they own. Quotations on it are removed with it.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await rfqService.DeleteAsync(id, currentUser.Id, ct);
        return NoContent();
    }

    /// <summary>Buyer: stop accepting quotations. The RFQ disappears from supplier browse.</summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(typeof(RfqDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RfqDetailDto>> Close(Guid id, CancellationToken ct)
        => Ok(await rfqService.SetStatusAsync(id, currentUser.Id, RfqStatus.Closed, ct));

    /// <summary>Buyer: put a closed RFQ back on the market.</summary>
    [HttpPost("{id:guid}/reopen")]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(typeof(RfqDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RfqDetailDto>> Reopen(Guid id, CancellationToken ct)
        => Ok(await rfqService.SetStatusAsync(id, currentUser.Id, RfqStatus.Open, ct));

    /// <summary>Buyer: the quotations received on their own RFQ, cheapest first.</summary>
    [HttpGet("{id:guid}/quotations")]
    [Authorize(Roles = UserRole.Buyer)]
    [ProducesResponseType(typeof(IReadOnlyList<QuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<QuotationDto>>> Quotations(Guid id, CancellationToken ct)
        => Ok(await quotationService.GetForRfqAsync(id, currentUser.Id, ct));

    /// <summary>Supplier: submit a quotation. One per supplier per RFQ.</summary>
    [HttpPost("{id:guid}/quotations")]
    [Authorize(Roles = UserRole.Supplier)]
    [ProducesResponseType(typeof(QuotationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuotationDto>> SubmitQuotation(
        Guid id, CreateQuotationRequest request, CancellationToken ct)
    {
        var quotation = await quotationService.SubmitAsync(id, currentUser.Id, request, ct);
        return Created($"/api/rfqs/{id}/quotations/{quotation.Id}", quotation);
    }
}
