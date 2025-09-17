namespace GameGuild.CQRS;

/// <summary> Validation result </summary>
public class ValidationResult {
  /// <summary> Whether the validation passed </summary>
  public bool IsValid { get => !Errors.Any(); }

  /// <summary> Collection of validation errors </summary>
  public IEnumerable<ValidationError> Errors { get; init; } = [];

  /// <summary> Creates a successful validation result </summary>
  public static ValidationResult Success() { return new ValidationResult(); }

  /// <summary> Creates a failed validation result </summary>
  /// <param name="errors"> Validation errors </param>
  public static ValidationResult Failure(params ValidationError[ ] errors) { return new ValidationResult { Errors = errors }; }

  /// <summary> Creates a failed validation result </summary>
  /// <param name="errors"> Validation errors </param>
  public static ValidationResult Failure(IEnumerable<ValidationError> errors) { return new ValidationResult { Errors = errors }; }
}
