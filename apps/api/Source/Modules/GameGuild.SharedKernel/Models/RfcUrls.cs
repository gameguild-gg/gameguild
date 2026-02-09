namespace GameGuild;

/// <summary>
///     Centralized RFC problem-type URLs for ProblemDetails responses.
///     Eliminates magic-string duplication across controllers, middleware, and result mappers.
/// </summary>
public static class RfcUrls
{
    /// <summary>RFC 9110 §15.5.1 — 400 Bad Request.</summary>
    public const string BadRequest = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1";

    /// <summary>RFC 9110 §15.5.4 — 403 Forbidden.</summary>
    public const string Forbidden = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.4";

    /// <summary>RFC 9110 §15.5.5 — 404 Not Found.</summary>
    public const string NotFound = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5";

    /// <summary>RFC 9110 §15.5.10 — 409 Conflict.</summary>
    public const string Conflict = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10";

    /// <summary>RFC 9110 §15.6.1 — 500 Internal Server Error.</summary>
    public const string InternalServerError = "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1";

    /// <summary>RFC 9110 §11.6.1 — 401 Unauthorized.</summary>
    public const string Unauthorized = "https://www.rfc-editor.org/rfc/rfc9110#section-11.6.1";

    /// <summary>RFC 4918 §11.2 — 422 Unprocessable Entity (also in RFC 9110 §15.5.21).</summary>
    public const string UnprocessableEntity = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.21";

    /// <summary>
    ///     Maps an <see cref="ErrorType" /> to its RFC problem-type URL.
    /// </summary>
    public static string ForErrorType(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => BadRequest,
        ErrorType.Problem => BadRequest,
        ErrorType.NotFound => NotFound,
        ErrorType.Conflict => Conflict,
        ErrorType.Unauthorized => Unauthorized,
        ErrorType.Forbidden => Forbidden,
        ErrorType.None => string.Empty,
        _ => InternalServerError
    };
}
