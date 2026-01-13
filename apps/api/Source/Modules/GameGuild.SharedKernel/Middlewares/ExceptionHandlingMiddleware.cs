using System.Net;
using System.Text.Json;
using GameGuild.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Middlewares;

/// <summary>
///     Middleware for handling unhandled exceptions globally.
///     Implements secure error handling with proper 401/403 distinction and information leakage prevention.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (SecurityException securityException)
        {
            // Log the internal message (with sensitive details) for debugging
            logger.LogWarning(securityException,
                "Security exception occurred. StatusCode: {StatusCode}, Path: {Path}, TraceId: {TraceId}, InternalMessage: {InternalMessage}",
                (int)securityException.StatusCode,
                context.Request.Path,
                context.TraceIdentifier,
                securityException.InternalMessage);

            // Return sanitized public message to client
            await HandleSecurityExceptionAsync(context, securityException);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleSecurityExceptionAsync(HttpContext context, SecurityException exception)
    {
        var response = context.Response;
        response.ContentType = "application/problem+json";
        response.StatusCode = (int)exception.StatusCode;

        var problemDetails = new
        {
            type = GetProblemTypeUrl(exception.StatusCode),
            title = GetProblemTitle(exception.StatusCode),
            status = (int)exception.StatusCode,
            // Use the sanitized public message - never expose internal details
            detail = exception.PublicMessage,
            traceId = context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await response.WriteAsync(jsonResponse);
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/problem+json";
        response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // SECURITY: Never expose exception details to clients
        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = 500,
            // Generic message - don't leak exception.Message
            detail = "An unexpected error occurred. Please try again later.",
            traceId = context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await response.WriteAsync(jsonResponse);
    }

    private static string GetProblemTypeUrl(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
        HttpStatusCode.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };

    private static string GetProblemTitle(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "Unauthorized",
        HttpStatusCode.Forbidden => "Forbidden",
        _ => "Internal Server Error"
    };
}
