using System.Net;
using System.Text.Json;
using GameGuild;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace GameGuild.Core.Middleware;

/// <summary>
/// Global exception handling middleware that converts exceptions to standardized ProblemDetails responses.
/// </summary>
public class GlobalExceptionMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment) {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        }
        catch (Exception exception) {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception) {
        var problemDetails = CreateProblemDetails(context, exception);
        var correlationId = context.Items["CorrelationId"]?.ToString();

        // Log the exception with correlation ID if available
        if (!string.IsNullOrEmpty(correlationId)) {
            _logger.LogError(exception,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Exception: {ExceptionType}",
                correlationId, exception.GetType().Name);
        }
        else {
            _logger.LogError(exception, "Unhandled exception occurred. Exception: {ExceptionType}",
                exception.GetType().Name);
        }

        // Set response headers
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        // Add correlation ID to response if available
        if (!string.IsNullOrEmpty(correlationId)) {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        // Serialize and write response
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(json);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception) {
        return exception switch {
            // DomainValidationException validationEx => new ProblemDetails {
            //     Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            //     Title = "Validation Error",
            //     Status = (int)HttpStatusCode.BadRequest,
            //     Detail = validationEx.Message,
            //     Instance = context.Request.Path,
            //     Extensions = { ["errors"] = validationEx.Errors }
            // },

            UnauthorizedAccessException => new ProblemDetails {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Unauthorized",
                Status = (int)HttpStatusCode.Unauthorized,
                Detail = "You are not authorized to access this resource.",
                Instance = context.Request.Path
            },

            ArgumentNullException argEx => new ProblemDetails {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = _environment.IsDevelopment()
                    ? $"Missing required parameter: {argEx.ParamName}"
                    : "Invalid request parameters.",
                Instance = context.Request.Path
            },

            ArgumentException argEx => new ProblemDetails {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = _environment.IsDevelopment()
                    ? argEx.Message
                    : "Invalid request parameters.",
                Instance = context.Request.Path
            },

            NotImplementedException => new ProblemDetails {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2",
                Title = "Not Implemented",
                Status = (int)HttpStatusCode.NotImplemented,
                Detail = "This feature is not yet implemented.",
                Instance = context.Request.Path
            },

            TimeoutException => new ProblemDetails {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
                Title = "Request Timeout",
                Status = (int)HttpStatusCode.RequestTimeout,
                Detail = "The request timed out. Please try again.",
                Instance = context.Request.Path
            },

            _ => new ProblemDetails {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An error occurred while processing your request.",
                Instance = context.Request.Path
            }
        };
    }
}

/// <summary>
/// Extension methods for registering the global exception middleware.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions {
    /// <summary>
    /// Adds the global exception handling middleware to the application pipeline.
    /// This should be added early in the pipeline to catch all unhandled exceptions.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
