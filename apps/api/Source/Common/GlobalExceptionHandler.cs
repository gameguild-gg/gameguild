using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Common;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
  : IExceptionHandler {
  public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken
  ) {
    logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

    var problemDetails = CreateProblemDetails(exception);

    httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
    httpContext.Response.ContentType = "application/problem+json";

    await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

    return true;
  }

  private static ProblemDetails CreateProblemDetails(Exception exception) {
    return exception switch {
      ValidationException validationException => new ProblemDetails {
        Status = StatusCodes.Status400BadRequest,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        Title = "Validation Error",
        Detail = validationException.Message,
        Extensions = new Dictionary<string, object?> { ["errors"] = new[] { validationException.Message } },
      },
      ArgumentException argumentException => new ProblemDetails { Status = StatusCodes.Status400BadRequest, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", Title = "Bad Request", Detail = argumentException.Message },
      InvalidOperationException invalidOperationException when
        invalidOperationException.Message.Contains("not found") => new ProblemDetails {
          Status = StatusCodes.Status404NotFound, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", Title = "Not Found", Detail = invalidOperationException.Message,
        },
      InvalidOperationException invalidOperationException when
        invalidOperationException.Message.Contains("Concurrency conflict") => new ProblemDetails {
          Status = StatusCodes.Status409Conflict, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8", Title = "Conflict", Detail = invalidOperationException.Message,
        },
      InvalidOperationException invalidOperationException when
        invalidOperationException.Message.Contains("already exists") =>
        new ProblemDetails { Status = StatusCodes.Status409Conflict, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8", Title = "Conflict", Detail = invalidOperationException.Message },
      UnauthorizedAccessException => new ProblemDetails {
        Status = StatusCodes.Status401Unauthorized, Type = "https://tools.ietf.org/html/rfc7235#section-3.1", Title = "Unauthorized", Detail = "Authentication is required to access this resource",
      },
      _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1", Title = "Server Error", Detail = "An unexpected error occurred while processing your request" },
    };
  }
}
