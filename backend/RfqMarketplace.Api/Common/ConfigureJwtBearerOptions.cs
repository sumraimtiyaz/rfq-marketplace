using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RfqMarketplace.Api.Common;

/// <summary>
/// Configures JWT bearer validation from <see cref="JwtOptions"/>.
///
/// This deliberately resolves the options from DI rather than reading IConfiguration inline in
/// Program.cs: the signing key must come from exactly one place, or the key used to *issue*
/// tokens can silently drift from the key used to *validate* them.
/// </summary>
public class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name is not JwtBearerDefaults.AuthenticationScheme)
            return;

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwt.Issuer,
            ValidAudience = _jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            // The browser app never sees the token, so fall back to the HTTP-only cookie when no
            // Authorization header was sent. API clients keep using the header as usual.
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(_jwt.CookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },

            // Answer with the same ProblemDetails shape the rest of the API uses, rather than
            // an empty body with a WWW-Authenticate header.
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteProblemAsync(context.Response, StatusCodes.Status401Unauthorized,
                    "Unauthorized", "You need to be signed in to do that.");
            },

            OnForbidden = context => WriteProblemAsync(context.Response, StatusCodes.Status403Forbidden,
                "Forbidden", "Your account role does not allow this action.")
        };
    }

    private static async Task WriteProblemAsync(HttpResponse response, int status, string title, string detail)
    {
        if (response.HasStarted)
            return;

        response.StatusCode = status;
        response.ContentType = "application/problem+json";
        await response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Detail = detail });
    }
}
