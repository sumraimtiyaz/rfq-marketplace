using System.Net;
using Microsoft.AspNetCore.Mvc;
using RfqMarketplace.Api.Common;

namespace RfqMarketplace.Api.Middleware;

/// <summary>
/// Turns exceptions into RFC 7807 ProblemDetails. Expected failures (<see cref="AppException"/>)
/// keep their message; anything else is logged in full and reported to the client as a generic
/// 500 so internal details never leak.
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogInformation("Handled {Exception} on {Path}: {Message}",
                ex.GetType().Name, context.Request.Path, ex.Message);
            await WriteAsync(context, (int)ex.StatusCode, ex.Title, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody left to answer.
            logger.LogDebug("Request to {Path} was cancelled by the client", context.Request.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteAsync(context,
                (int)HttpStatusCode.InternalServerError,
                "Server error",
                env.IsDevelopment() ? ex.Message : "Something went wrong. Please try again.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem);
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionMiddleware>();
}
