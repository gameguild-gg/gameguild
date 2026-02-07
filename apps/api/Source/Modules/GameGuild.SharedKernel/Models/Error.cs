namespace GameGuild.Models;

/// <summary>
///     Represents a domain error with a machine-readable code, human-readable description, and type.
///     Use the static factory methods to create errors of the appropriate type.
/// </summary>
public record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>Sentinel value representing "no error".</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Generic null-value error.</summary>
    public static readonly Error NullValue = new("General.Null", "Null value was provided", ErrorType.Failure);

    public string Code { get; } = Code;

    public string Description { get; } = Description;

    public ErrorType Type { get; } = Type;

    // ── Factory methods ──────────────────────────────────────────────────

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Problem(string code, string description) => new(code, description, ErrorType.Problem);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
}
