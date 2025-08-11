namespace GameGuild.Common;

/// <summary>
/// Custom validation exception for better error handling
/// </summary>
public class ValidationException : Exception {
  public ValidationException(string message) : base(message) { }

  public ValidationException(string message, Exception innerException) : base(message, innerException) { }

  public ValidationException(IEnumerable<string> errors) : base(string.Join("; ", errors)) { Errors = errors.ToList(); }

  public List<string> Errors { get; } = [];
}
