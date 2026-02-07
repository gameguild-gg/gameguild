using GameGuild.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Controllers;

/// <summary>
///     Base controller providing standardized Result-to-ActionResult mapping.
///     All API controllers should inherit from this to ensure consistent error handling,
///     HTTP status code mapping, and response envelope formatting.
/// </summary>
/// <remarks>
///     This eliminates the duplicated if/else error-handling pattern found across controllers
///     and ensures all responses go through the same mapping pipeline.
/// </remarks>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    // ── Result<T> → ActionResult mapping ─────────────────────────────────

    /// <summary>
    ///     Maps a <see cref="Result{T}" /> to an <see cref="ActionResult{T}" />.
    ///     Returns 200 OK on success, or the appropriate error status code on failure.
    /// </summary>
    protected ActionResult<T> ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return ToProblemResult(result.Error);
    }

    /// <summary>
    ///     Maps a <see cref="Result{T}" /> to an <see cref="ActionResult{T}" /> with a 201 Created response.
    ///     Use for POST endpoints that create resources.
    /// </summary>
    protected ActionResult<T> ToCreatedResult<T>(Result<T> result, string? routeName = null, object? routeValues = null)
    {
        if (result.IsSuccess)
        {
            if (routeName is not null)
                return CreatedAtRoute(routeName, routeValues, result.Value);

            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToProblemResult(result.Error);
    }

    /// <summary>
    ///     Maps a non-generic <see cref="Result" /> to an <see cref="IActionResult" />.
    ///     Returns 204 No Content on success, or the appropriate error status code on failure.
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return ToProblemActionResult(result.Error);
    }

    // ── Error mapping ────────────────────────────────────────────────────

    /// <summary>
    ///     Converts an <see cref="Error" /> to a ProblemDetails <see cref="ObjectResult" />.
    /// </summary>
    private ObjectResult ToProblemResult(Error error)
    {
        var statusCode = CustomResults.GetStatusCode(error.Type);
        var problemDetails = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description,
            Status = statusCode,
            Type = GetRfcType(error.Type)
        };

        if (error is ValidationError validationError)
        {
            problemDetails.Extensions["errors"] = validationError.Errors;
        }

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    /// <summary>
    ///     Converts an <see cref="Error" /> to a ProblemDetails <see cref="IActionResult" />.
    /// </summary>
    private IActionResult ToProblemActionResult(Error error) => ToProblemResult(error);

    private static string GetRfcType(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
            ErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}
