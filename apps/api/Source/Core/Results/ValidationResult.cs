using Microsoft.IdentityModel.Tokens.Experimental;


namespace GameGuild;

/// <summary> Validation result containing success status and error messages </summary>
public class ValidationResult {
  public bool IsValid { get; private set; }

  public List<ValidationError> Errors { get; private init; } = [];

  public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

  public static ValidationResult Success() { return new ValidationResult { IsValid = true }; }

  public static ValidationResult Failure(params ValidationError[ ] errors) { return new ValidationResult { IsValid = false, Errors = errors.ToList() }; }

  public static ValidationResult Failure(string errorMessage, string? propertyName = null) { return new ValidationResult { IsValid = false, Errors = [new ValidationError(errorMessage, propertyName)] }; }

  public ValidationResult WithMetadata(string key, object value) {
    Metadata[key] = value;

    return this;
  }

  public void AddError(string message, string? propertyName = null) {
    Errors.Add(new ValidationError(message, propertyName));
    IsValid = false;
  }

  public void AddErrors(IEnumerable<ValidationError> errors) {
    Errors.AddRange(errors);
    if (Errors.Any()) IsValid = false;
  }

  public string GetErrorsAsString(string separator = "; ") { return string.Join(separator, Errors.Select(e => e.Message)); }
}
