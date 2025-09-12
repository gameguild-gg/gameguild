namespace GameGuild;

/// <summary>
///     Exception thrown when domain validation fails
/// </summary>
public class DomainValidationException : DomainException {
  public DomainValidationException(ValidationResult validationResult) : base(validationResult.GetErrorsAsString()) { ValidationResult = validationResult; }

  public DomainValidationException(string message, ValidationResult validationResult) : base(message) { ValidationResult = validationResult; }

  public DomainValidationException(string message, ValidationResult validationResult, Exception innerException) : base(message, innerException) { ValidationResult = validationResult; }

  public ValidationResult ValidationResult { get; }
}
