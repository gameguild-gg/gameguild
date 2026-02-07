namespace GameGuild.Models;

/// <summary>
///     Categorizes domain errors so they can be mapped to HTTP status codes or similar transport-level responses.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Problem = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6
}
