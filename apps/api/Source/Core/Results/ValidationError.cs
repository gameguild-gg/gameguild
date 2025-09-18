namespace GameGuild;

/// <summary>
/// Legacy validation error type. 
/// Note: This is being phased out in favor of the unified Error type.
/// New code should use Error.ValidationFailure() instead.
/// </summary>
[Obsolete("Use Error.ValidationFailure() instead. This type will be removed in a future version.")]
public record ValidationError(string Message, string? PropertyName = null, object? AttemptedValue = null) {
    /// <summary>
    /// Converts this ValidationError to the new unified Error type.
    /// </summary>
    /// <returns>An Error instance representing this validation failure.</returns>
    public Error ToError() {
        return Error.ValidationFailure(PropertyName ?? "Unknown", Message, AttemptedValue);
    }

    /// <summary>
    /// Creates a ValidationError from an Error.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <returns>A ValidationError instance.</returns>
    public static ValidationError FromError(Error error) {
        var propertyName = error.Metadata?.GetValueOrDefault("property")?.ToString();
        var attemptedValue = error.Metadata?.GetValueOrDefault("attemptedValue");

        return new ValidationError(error.Message, propertyName, attemptedValue);
    }
}
