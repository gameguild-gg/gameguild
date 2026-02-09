namespace GameGuild.CQRS;

/// <summary>
///     Result of CQRS pipeline validation. Immutable — use factory methods to create.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>Whether the validation passed (no errors).</summary>
    public bool IsValid => !Errors.Any();

    /// <summary>Collection of validation errors.</summary>
    public IEnumerable<ValidationError> Errors { get; init; } = [];

    /// <summary>Creates a successful validation result.</summary>
    public static ValidationResult Success() => new();

    /// <summary>Creates a failed validation result with the specified errors.</summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new() { Errors = errors };

    /// <summary>Creates a failed validation result from a collection of errors.</summary>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new() { Errors = errors };
}
