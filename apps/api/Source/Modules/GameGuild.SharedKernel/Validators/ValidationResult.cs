using GameGuild.CQRS;

namespace GameGuild.Validators;

/// <summary>
///     Domain-level validation result with metadata support and mutable error accumulation.
///     For CQRS pipeline validation, use <see cref="GameGuild.CQRS.ValidationResult" /> instead.
/// </summary>
/// <remarks>
///     This type intentionally differs from the CQRS ValidationResult:
///     - Supports mutable error accumulation via <see cref="AddError" /> / <see cref="AddErrors" />
///     - Carries arbitrary <see cref="Metadata" /> for domain-specific context
///     - Used in domain entities and services for complex multi-step validation
/// </remarks>
public class ValidationResult
{
    public bool IsValid { get; private set; }

    public List<ValidationError> Errors { get; init; } = [];

    /// <summary>Arbitrary metadata for domain-specific validation context.</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(params ValidationError[] errors)
        => new() { IsValid = false, Errors = errors.ToList() };

    public static ValidationResult Failure(string errorMessage, string? propertyName = null)
        => new() { IsValid = false, Errors = [new ValidationError(errorMessage, propertyName)] };

    /// <summary>
    ///     Creates a domain <see cref="ValidationResult" /> from a CQRS pipeline <see cref="GameGuild.CQRS.ValidationResult" />.
    /// </summary>
    public static ValidationResult FromCqrsResult(GameGuild.CQRS.ValidationResult cqrsResult)
    {
        if (cqrsResult.IsValid) return Success();
        return new ValidationResult
        {
            IsValid = false,
            Errors = cqrsResult.Errors.Select(e => new ValidationError(e.ErrorMessage, e.PropertyName)).ToList()
        };
    }

    /// <summary>
    ///     Converts this domain validation result to a CQRS pipeline <see cref="GameGuild.CQRS.ValidationResult" />.
    /// </summary>
    public GameGuild.CQRS.ValidationResult ToCqrsResult()
    {
        if (IsValid) return GameGuild.CQRS.ValidationResult.Success();
        return GameGuild.CQRS.ValidationResult.Failure(
            Errors.Select(e => new GameGuild.CQRS.ValidationError(e.PropertyName ?? string.Empty, e.Message)).ToArray());
    }

    public ValidationResult WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    public void AddError(string message, string? propertyName = null)
    {
        Errors.Add(new ValidationError(message, propertyName));
        IsValid = false;
    }

    public void AddErrors(IEnumerable<ValidationError> errors)
    {
        Errors.AddRange(errors);
        if (Errors.Count > 0) IsValid = false;
    }

    /// <summary>Concatenates all error messages into a single string.</summary>
    public string GetErrorsAsString(string separator = "; ")
        => string.Join(separator, Errors.Select(e => e.Message));
}
