namespace GameGuild.CQRS;

/// <summary>
///     Validation error
/// </summary>
public record ValidationError(string PropertyName, string ErrorMessage, object? AttemptedValue = null);
