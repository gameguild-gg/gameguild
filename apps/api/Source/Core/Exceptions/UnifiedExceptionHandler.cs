using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Core;

/// <summary>
/// Unified exception handler that works with the new Result pattern and exception hierarchy
/// Replaces the existing GlobalExceptionHandler and ExceptionHandlingMiddleware
/// </summary>
internal sealed class UnifiedExceptionHandler(ILogger<UnifiedExceptionHandler> logger) : IExceptionHandler {

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {
        logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = CreateProblemDetails(exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails), cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception) {
        return exception switch {
            ValidationException validationException => new ValidationProblemDetails(
                validationException.Errors.Select((error, index) => new KeyValuePair<string, string[]>(
                    $"error{index}", new[] { error }
                )).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            ) {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Extensions = new Dictionary<string, object?> {
                    ["errors"] = validationException.Errors
                }
            },

            BusinessRuleViolationException businessRuleException => new ProblemDetails {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Business Rule Violation",
                Detail = businessRuleException.Message,
                Extensions = new Dictionary<string, object?> {
                    ["rule"] = businessRuleException.Rule,
                    ["context"] = businessRuleException.Context
                }
            },

            // DomainValidationException domainValidationException => new ValidationProblemDetails(
            //     domainValidationException.ValidationResult.Errors.Select((error, index) => new KeyValuePair<string, string[]>(
            //         error.PropertyName ?? $"error{index}", new[] { error.Message }
            //     )).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            // ) {
            //     Status = StatusCodes.Status400BadRequest,
            //     Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            //     Title = "Domain Validation Error",
            //     Detail = domainValidationException.Message
            // },

            DomainException domainException => new ProblemDetails {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Domain Error",
                Detail = domainException.Message
            },

            ArgumentException argumentException => new ProblemDetails {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Detail = argumentException.Message
            },

            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("not found") => new ProblemDetails {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Detail = invalidOperationException.Message
            },

            UnauthorizedAccessException => new ProblemDetails {
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Unauthorized",
                Detail = "The request requires user authentication."
            },

            _ => new ProblemDetails {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred."
            }
        };
    }
}
