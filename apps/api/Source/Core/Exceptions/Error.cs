namespace GameGuild;

/// <summary>
/// Unified error representation for all failure scenarios in the application.
/// Replaces the need for separate ValidationError, BusinessRuleError, etc.
/// Use with Result<T> pattern instead of throwing exceptions.
/// </summary>
public record Error(string Code, string Message, ErrorType Type, Dictionary<string, object>? Metadata = null) {
  public static readonly Error None = new("", "", ErrorType.Failure);

  public static readonly Error NullValue = new("General.Null", "Null value was provided", ErrorType.Failure);

  // Factory methods for different error types
  public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

  public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

  public static Error Problem(string code, string message) => new(code, message, ErrorType.Problem);

  public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

  public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

  // Validation-specific factory methods (replaces ValidationError concept)
  public static Error ValidationFailure(string propertyName, string message, object? attemptedValue = null) {
    var metadata = attemptedValue != null
      ? new Dictionary<string, object> { ["attemptedValue"] = attemptedValue, ["property"] = propertyName }
      : new Dictionary<string, object> { ["property"] = propertyName };

    return new($"Validation.{propertyName}", message, ErrorType.Validation, metadata);
  }

  public static Error RequiredField(string propertyName)
    => ValidationFailure(propertyName, $"{propertyName} is required");

  public static Error InvalidFormat(string propertyName, object? attemptedValue = null)
    => ValidationFailure(propertyName, $"{propertyName} has invalid format", attemptedValue);

  public static Error OutOfRange(string propertyName, object? attemptedValue = null)
    => ValidationFailure(propertyName, $"{propertyName} is out of valid range", attemptedValue);

  // Business rule factory methods (replaces BusinessRuleViolationException concept)
  public static Error BusinessRule(string rule, string message, object? context = null) {
    var metadata = context != null
      ? new Dictionary<string, object> { ["rule"] = rule, ["context"] = context }
      : new Dictionary<string, object> { ["rule"] = rule };

    return new($"BusinessRule.{rule}", message, ErrorType.Problem, metadata);
  }

  // Convenience methods for metadata access
  public string? GetProperty() => Metadata?.TryGetValue("property", out var prop) == true ? prop.ToString() : null;
  public object? GetAttemptedValue() => Metadata?.TryGetValue("attemptedValue", out var value) == true ? value : null;
  public string? GetRule() => Metadata?.TryGetValue("rule", out var rule) == true ? rule.ToString() : null;
  public object? GetContext() => Metadata?.TryGetValue("context", out var context) == true ? context : null;

  // For backward compatibility during transition
  [Obsolete("Use Message property instead")]
  public string Description => Message;
}
