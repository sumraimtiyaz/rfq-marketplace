using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RfqMarketplace.Api.Common;
using RfqMarketplace.Api.DTOs.Auth;
using RfqMarketplace.Api.Services;

namespace RfqMarketplace.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ICurrentUser currentUser,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    /// <summary>Creates a Buyer or Supplier account and signs the new user in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        Response.SetAuthCookie(_jwt, result.Token, result.ExpiresAt);

        // 201 without a Location header: an account has no addressable resource URL of its own,
        // and the caller already has the profile in the body.
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Exchanges credentials for a session cookie (and a bearer token for API clients).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        Response.SetAuthCookie(_jwt, result.Token, result.ExpiresAt);
        return Ok(result);
    }

    /// <summary>Clears the session cookie. Safe to call when already signed out.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.ClearAuthCookie(_jwt);
        return NoContent();
    }

    /// <summary>Returns the signed-in user, including their role. Used by the app to route on load.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
        => Ok(await authService.GetCurrentUserAsync(currentUser.Id, ct));
}
