using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild;

/// <summary>
///     Centralizes the mapping from <see cref="Error" /> to <see cref="ProblemDetails" />.
///     Both <see cref="CustomResults" /> (minimal APIs) and <see cref="BaseApiController" /> (MVC)
///     delegate here so the mapping logic lives in exactly one place (DRY).
/// </summary>
public static class ProblemDetailsMapper
{
    /// <summary>
    ///     Maps an <see cref="Error" /> to a fully populated <see cref="ProblemDetails" /> instance
    ///     with RFC 7807 type URL, HTTP status code, and optional validation errors extension.
    /// </summary>
    /// <param name="error">The domain error to map.</param>
    /// <returns>A <see cref="ProblemDetails" /> ready for serialization.</returns>
    public static ProblemDetails ToProblemDetails(Error error)
    {
        var statusCode = CustomResults.GetStatusCode(error.Type);
        var problemDetails = new ProblemDetails
        {
            Title = GetTitle(error),
            Detail = GetDetail(error),
            Status = statusCode,
            Type = RfcUrls.ForErrorType(error.Type)
        };

        if (error is AggregateValidationError validationError)
        {
            problemDetails.Extensions["errors"] = validationError.Errors;
        }

        return problemDetails;
    }

    /// <summary>
    ///     Returns the ProblemDetails title for the given error.
    ///     Known error types use the error code; unknown/server errors use a generic title.
    /// </summary>
    private static string GetTitle(Error error) =>
        error.Type is ErrorType.Validation or ErrorType.Problem or ErrorType.NotFound
            or ErrorType.Conflict or ErrorType.Unauthorized or ErrorType.Forbidden or ErrorType.None
            ? error.Code
            : "Server failure";

    /// <summary>
    ///     Returns the ProblemDetails detail for the given error.
    ///     Known error types use the error description; unknown/server errors use a generic message.
    /// </summary>
    private static string GetDetail(Error error) =>
        error.Type is ErrorType.Validation or ErrorType.Problem or ErrorType.NotFound
            or ErrorType.Conflict or ErrorType.Unauthorized or ErrorType.Forbidden or ErrorType.None
            ? error.Description
            : "An unexpected error occurred";
}
