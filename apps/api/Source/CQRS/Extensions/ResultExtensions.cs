using Microsoft.AspNetCore.Mvc;

namespace GameGuild.CQRS;

/// <summary>
/// Extension methods for converting Result<T> to ActionResult for API responses
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an ActionResult
    /// </summary>
    /// <param name="result">The result to convert</param>
    /// <returns>An appropriate ActionResult based on the result</returns>
    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new OkResult();
        }

        return result.Error!.Code switch
        {
            var code when code.StartsWith("NotFound.") => new NotFoundObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            }),
            var code when code.StartsWith("Validation.") => new BadRequestObjectResult(new ValidationProblemDetails(
                result.ValidationErrors.ToDictionary(
                    error => error.PropertyName ?? "General",
                    error => new[] { error.ErrorMessage }
                ))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            }),
            var code when code.StartsWith("Conflict.") => new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            }),
            var code when code.StartsWith("Unauthorized.") => new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            }),
            var code when code.StartsWith("Forbidden.") => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            })
            { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            })
            { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }

    /// <summary>
    /// Converts a Result<T> to an ActionResult<T>
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="result">The result to convert</param>
    /// <returns>An appropriate ActionResult<T> based on the result</returns>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return result.Error!.Code switch
        {
            var code when code.StartsWith("NotFound.") => new NotFoundObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            }),
            var code when code.StartsWith("Validation.") => new BadRequestObjectResult(new ValidationProblemDetails(
                result.ValidationErrors.ToDictionary(
                    error => error.PropertyName ?? "General",
                    error => new[] { error.ErrorMessage }
                ))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            }),
            var code when code.StartsWith("Conflict.") => new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            }),
            var code when code.StartsWith("Unauthorized.") => new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            }),
            var code when code.StartsWith("Forbidden.") => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            })
            { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            })
            { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }

    /// <summary>
    /// Executes an action if the result is successful, otherwise returns the error as ActionResult
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="result">The result to check</param>
    /// <param name="onSuccess">Action to execute if successful</param>
    /// <returns>ActionResult based on the result</returns>
    public static ActionResult Match<T>(this Result<T> result, Func<T, ActionResult> onSuccess)
    {
        return result.IsSuccess ? onSuccess(result.Value!) : result.ToActionResult();
    }

    /// <summary>
    /// Executes an action if the result is successful, otherwise returns the error as ActionResult<TResult>
    /// </summary>
    /// <typeparam name="T">The type of the source value</typeparam>
    /// <typeparam name="TResult">The type of the result value</typeparam>
    /// <param name="result">The result to check</param>
    /// <param name="onSuccess">Function to execute if successful</param>
    /// <returns>ActionResult<TResult> based on the result</returns>
    public static ActionResult<TResult> Match<T, TResult>(this Result<T> result, Func<T, TResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(onSuccess(result.Value!));
        }

        return result.Error!.Code switch
        {
            var code when code.StartsWith("NotFound.") => new NotFoundObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            }),
            var code when code.StartsWith("Validation.") => new BadRequestObjectResult(new ValidationProblemDetails(
                result.ValidationErrors.ToDictionary(
                    error => error.PropertyName ?? "General",
                    error => new[] { error.ErrorMessage }
                ))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            }),
            var code when code.StartsWith("Conflict.") => new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            }),
            var code when code.StartsWith("Unauthorized.") => new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            }),
            var code when code.StartsWith("Forbidden.") => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            })
            { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = result.Error.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            })
            { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}
