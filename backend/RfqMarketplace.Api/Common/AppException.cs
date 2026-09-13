using System.Net;

namespace RfqMarketplace.Api.Common;

/// <summary>
/// Base for expected, business-rule failures. The exception middleware turns these into
/// ProblemDetails with the right status code, so services never need to know about HTTP.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, HttpStatusCode statusCode) : base(message)
        => StatusCode = statusCode;

    public HttpStatusCode StatusCode { get; }

    public virtual string Title => StatusCode switch
    {
        HttpStatusCode.NotFound => "Not found",
        HttpStatusCode.Forbidden => "Forbidden",
        HttpStatusCode.Conflict => "Conflict",
        HttpStatusCode.Unauthorized => "Unauthorized",
        _ => "Request failed"
    };
}

public sealed class NotFoundException(string message = "The requested resource was not found.")
    : AppException(message, HttpStatusCode.NotFound);

/// <summary>Authenticated and the right role, but the resource belongs to someone else.</summary>
public sealed class ForbiddenException(string message = "You do not have permission to access this resource.")
    : AppException(message, HttpStatusCode.Forbidden);

public sealed class ConflictException(string message) : AppException(message, HttpStatusCode.Conflict);

/// <summary>A business rule rejected the request (e.g. quoting on an expired RFQ).</summary>
public sealed class BusinessRuleException(string message) : AppException(message, HttpStatusCode.BadRequest);

public sealed class AuthenticationFailedException(string message = "Invalid email or password.")
    : AppException(message, HttpStatusCode.Unauthorized);
