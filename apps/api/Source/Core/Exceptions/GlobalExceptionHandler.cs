using System.Security;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Core.Infrastructure.Exceptions;

/// <summary> Global exception handler for Clean Architecture following infrastructure concerns Provides centralized exception handling with proper HTTP status code mapping </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler {
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {
    logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

    var problemDetails = CreateProblemDetails(exception);

    httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
    httpContext.Response.ContentType = "application/problem+json";

    await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

    return true;
  }

  private static ProblemDetails CreateProblemDetails(Exception exception) {
    return exception switch {
      ValidationException validationException =>
        new ProblemDetails {
          Status = StatusCodes.Status400BadRequest,
          Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
          Title = "Validation Error",
          Detail = validationException.Message,
          Extensions = new Dictionary<string, object?> { ["errors"] = new[] { validationException.Message } },
        },
      ArgumentException argumentException => new ProblemDetails { Status = StatusCodes.Status400BadRequest, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", Title = "Bad Request", Detail = argumentException.Message },
      InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("not found") =>
        new ProblemDetails { Status = StatusCodes.Status404NotFound, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", Title = "Not Found", Detail = invalidOperationException.Message },
      InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("Concurrency conflict") =>
        new ProblemDetails { Status = StatusCodes.Status409Conflict, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8", Title = "Conflict", Detail = invalidOperationException.Message },
      InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("already exists") =>
        new ProblemDetails { Status = StatusCodes.Status409Conflict, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8", Title = "Conflict", Detail = invalidOperationException.Message },
      UnauthorizedAccessException =>
        new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Type = "https://tools.ietf.org/html/rfc7235#section-3.1", Title = "Unauthorized", Detail = "Authentication is required to access this resource" },
      SecurityException securityException => new ProblemDetails { Status = StatusCodes.Status403Forbidden, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3", Title = "Forbidden", Detail = securityException.Message },
      TimeoutException => new ProblemDetails { Status = StatusCodes.Status408RequestTimeout, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7", Title = "Request Timeout", Detail = "The request took too long to process" },
      NotSupportedException notSupportedException => new ProblemDetails {
        Status = StatusCodes.Status501NotImplemented,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2",
        Title = "Not Implemented",
        Detail = notSupportedException.Message,
      },
      _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1", Title = "Server Error", Detail = "An unexpected error occurred while processing your request" },
    };
  }
}
