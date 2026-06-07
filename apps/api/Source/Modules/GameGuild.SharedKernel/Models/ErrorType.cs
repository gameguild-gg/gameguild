namespace GameGuild;

/// <summary>
///     Categorizes domain errors so they can be mapped to HTTP status codes or similar transport-level responses.
/// </summary>
public enum ErrorType
{
    /// <summary>Sentinel value representing "no error". Used only by <see cref="Error.None"/>.</summary>
    None = -1,

    /// <summary>General / unexpected failure (maps to 500 Internal Server Error).</summary>
    Failure = 0,

    /// <summary>Input validation failure (maps to 400 Bad Request).</summary>
    Validation = 1,

    /// <summary>Domain-level business rule violation (maps to 400 Bad Request).</summary>
    Problem = 2,

    /// <summary>Requested resource was not found (maps to 404 Not Found).</summary>
    NotFound = 3,

    /// <summary>Conflicting state prevents the operation (maps to 409 Conflict).</summary>
    Conflict = 4,

    /// <summary>Caller is not authenticated (maps to 401 Unauthorized).</summary>
    Unauthorized = 5,

    /// <summary>Caller is authenticated but lacks permission (maps to 403 Forbidden).</summary>
    Forbidden = 6
}
