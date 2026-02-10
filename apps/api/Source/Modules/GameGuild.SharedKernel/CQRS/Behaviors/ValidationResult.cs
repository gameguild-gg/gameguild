namespace GameGuild.CQRS;

/// <summary>
///     Represents the result of a validation operation.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    ///     Whether the validation passed.
    /// </summary>
    public bool IsValid { get; private init; }

    /// <summary>
    ///     Validation errors, if any.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; private init; } = [];

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    ///     Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new() { IsValid = false, Errors = errors };
}
