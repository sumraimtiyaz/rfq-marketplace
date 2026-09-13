using Microsoft.AspNetCore.Identity;
using RfqMarketplace.Api.Common;
using RfqMarketplace.Api.DTOs.Auth;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = request.Name.Trim(),
            CompanyName = request.CompanyName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            // Identity's own password/email rules (length, complexity, uniqueness).
            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new BusinessRuleException(message);
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            // Never leave a user behind without a role; they would be authenticated but unable to do anything.
            await userManager.DeleteAsync(user);
            throw new BusinessRuleException("Could not assign the selected role.");
        }

        logger.LogInformation("Registered {Role} {UserId}", request.Role, user.Id);

        return BuildResponse(user, request.Role);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        // Same error for "no such user" and "wrong password" so the endpoint cannot be used
        // to discover which emails are registered.
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            throw new AuthenticationFailedException();
        }

        var role = await GetRoleAsync(user);
        return BuildResponse(user, role);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        return ToDto(user, await GetRoleAsync(user));
    }

    private async Task<string> GetRoleAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return roles.FirstOrDefault()
            ?? throw new BusinessRuleException("This account has no role assigned. Contact support.");
    }

    private AuthResponse BuildResponse(ApplicationUser user, string role)
    {
        var (token, expiresAt) = tokenService.CreateToken(user, role);
        return new AuthResponse(ToDto(user, role), token, expiresAt);
    }

    private static UserDto ToDto(ApplicationUser user, string role) => new(
        user.Id, user.Name, user.CompanyName, user.Email ?? string.Empty, role, user.CreatedAt);
}
