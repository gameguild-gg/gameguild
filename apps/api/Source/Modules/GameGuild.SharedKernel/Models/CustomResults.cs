using Microsoft.AspNetCore.Http;

namespace GameGuild.Models;

/// <summary>
///     Maps <see cref="Result" /> failures to RFC 7807 ProblemDetails responses.
///     Centralizes the error-to-HTTP mapping so controllers never need ad-hoc translation.
/// </summary>
public static class CustomResults
{
    /// <summary>
    ///     Converts a failed <see cref="Result" /> into an <see cref="IResult" /> ProblemDetails response.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is a success.</exception>
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot create a problem result from a successful result.");

        return Results.Problem(
            title: GetTitle(result.Error),
            detail: GetDetail(result.Error),
            type: GetRfcType(result.Error.Type),
            statusCode: GetStatusCode(result.Error.Type),
            extensions: GetErrors(result));
    }

    /// <summary>Maps an <see cref="ErrorType" /> to an HTTP status code.</summary>
    public static int GetStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Problem => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitle(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => error.Code,
            ErrorType.Problem => error.Code,
            ErrorType.NotFound => error.Code,
            ErrorType.Conflict => error.Code,
            ErrorType.Unauthorized => error.Code,
            ErrorType.Forbidden => error.Code,
            _ => "Server failure"
        };
    }

    private static string GetDetail(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => error.Description,
            ErrorType.Problem => error.Description,
            ErrorType.NotFound => error.Description,
            ErrorType.Conflict => error.Description,
            ErrorType.Unauthorized => error.Description,
            ErrorType.Forbidden => error.Description,
            _ => "An unexpected error occurred"
        };
    }

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

    private static Dictionary<string, object?>? GetErrors(Result result)
        => result.Error is not ValidationError validationError
            ? null
            : new Dictionary<string, object?> { { "errors", validationError.Errors } };
}
