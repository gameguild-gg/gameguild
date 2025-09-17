namespace GameGuild.CQRS;

/// <summary> Exception thrown when validation fails </summary>
public class ValidationException : Exception {
  /// <summary> Initializes a new instance of the ValidationException class </summary>
  public ValidationException() : base("One or more validation failures occurred.") { Errors = []; }

  /// <summary> Initializes a new instance of the ValidationException class </summary>
  /// <param name="message"> Error message </param>
  public ValidationException(string message) : base(message) { Errors = []; }

  /// <summary> Initializes a new instance of the ValidationException class </summary>
  /// <param name="message"> Error message </param>
  /// <param name="innerException"> Inner exception </param>
  public ValidationException(string message, Exception innerException) : base(message, innerException) { Errors = []; }

  /// <summary> Initializes a new instance of the ValidationException class </summary>
  /// <param name="errors"> Validation errors </param>
  public ValidationException(IEnumerable<ValidationError> errors) : base("One or more validation failures occurred.") { Errors = errors.ToList().AsReadOnly(); }

  /// <summary> Validation errors </summary>
  public IReadOnlyList<ValidationError> Errors { get; }
}
