using Microsoft.AspNetCore.Mvc;

namespace GameGuild;

/// <summary> Extension methods for converting Result<T> to ActionResult for API responses </summary>
public static class ResultExtensions {
  /// <summary> Converts a Result to an ActionResult </summary>
  /// <param name="result"> The result to convert </param>
  /// <returns> An appropriate ActionResult based on the result </returns>
  public static ActionResult ToActionResult(this Result result) {
    if (result.IsSuccess) { return new OkResult(); }

    return CreateActionResultFromError(result.Error);
  }

  /// <summary> Converts a Result<T> to an ActionResult<T> </summary>
  /// <typeparam name="T"> The type of the value </typeparam>
  /// <param name="result"> The result to convert </param>
  /// <returns> An appropriate ActionResult<T> based on the result </returns>
  public static ActionResult<T> ToActionResult<T>(this Result<T> result) {
    if (result.IsSuccess) { return new OkObjectResult(result.Value); }

    return CreateActionResultFromError(result.Error);
  }

  private static ActionResult CreateActionResultFromError(Error error) {
    switch (error.Type) {
      case ErrorType.NotFound:
        return new NotFoundObjectResult(new ProblemDetails {
          Status = StatusCodes.Status404NotFound,
          Title = "Not Found",
          Detail = error.Message,
          Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
        });

      case ErrorType.Validation:
        var problemDetails = new ValidationProblemDetails {
          Status = StatusCodes.Status400BadRequest,
          Title = "Validation Error",
          Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        };

        var propertyName = error.GetProperty() ?? "General";
        problemDetails.Errors[propertyName] = [error.Message];

        return new BadRequestObjectResult(problemDetails);

      case ErrorType.Conflict:
        return new ConflictObjectResult(new ProblemDetails {
          Status = StatusCodes.Status409Conflict,
          Title = "Conflict",
          Detail = error.Message,
          Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
        });

      case ErrorType.Problem:
        return new ObjectResult(new ProblemDetails {
          Status = StatusCodes.Status422UnprocessableEntity,
          Title = "Business Rule Violation",
          Detail = error.Message,
          Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
        }) {
          StatusCode = StatusCodes.Status422UnprocessableEntity,
        };

      default:
        return new ObjectResult(new ProblemDetails {
          Status = StatusCodes.Status500InternalServerError,
          Title = "Internal Server Error",
          Detail = error.Message,
          Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        }) {
          StatusCode = StatusCodes.Status500InternalServerError,
        };
    }
  }

  /// <summary> Executes an action if the result is successful, otherwise returns the error as ActionResult </summary>
  /// <typeparam name="T"> The type of the value </typeparam>
  /// <param name="result"> The result to check </param>
  /// <param name="onSuccess"> Action to execute if successful </param>
  /// <returns> ActionResult based on the result </returns>
  public static ActionResult Match<T>(this Result<T> result, Func<T, ActionResult> onSuccess) {
    return result.IsSuccess ? onSuccess(result.Value!) : CreateActionResultFromError(result.Error);
  }
}
