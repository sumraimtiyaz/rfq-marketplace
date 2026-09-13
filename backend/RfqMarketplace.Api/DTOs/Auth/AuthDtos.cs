namespace RfqMarketplace.Api.DTOs.Auth;

public record RegisterRequest(
    string Name,
    string CompanyName,
    string Email,
    string Password,
    string Role);

public record LoginRequest(string Email, string Password);

/// <summary>The shape returned by /api/auth/me and by register/login.</summary>
public record UserDto(
    Guid Id,
    string Name,
    string CompanyName,
    string Email,
    string Role,
    DateTimeOffset CreatedAt);

/// <summary>
/// The JWT is also returned in the body so Swagger and API clients can use bearer auth;
/// the browser app relies on the HTTP-only cookie set alongside it.
/// </summary>
public record AuthResponse(UserDto User, string Token, DateTimeOffset ExpiresAt);
