using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfqMarketplace.Api.Data;

namespace RfqMarketplace.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController(AppDbContext db) : ControllerBase
{
    /// <summary>Liveness plus a database round-trip, for container and load-balancer probes.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dbReachable = await db.Database.CanConnectAsync(ct);

        return dbReachable
            ? Ok(new { status = "healthy", database = "up", timestamp = DateTimeOffset.UtcNow })
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "degraded", database = "down", timestamp = DateTimeOffset.UtcNow });
    }
}
