namespace GameGuild.CQRS;

/// <summary>
///     Exception thrown when request validation fails.
///     Named RequestValidationException to distinguish from FluentValidation.ValidationException.
/// </summary>
public class RequestValidationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the RequestValidationException class
    /// </summary>
    public RequestValidationException() : base("One or more validation failures occurred.") { Errors = []; }

    /// <summary>
    ///     Initializes a new instance of the RequestValidationException class
    /// </summary>
    /// <param name="message">Error message</param>
    public RequestValidationException(string message) : base(message) { Errors = []; }

    /// <summary>
    ///     Initializes a new instance of the RequestValidationException class
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public RequestValidationException(string message, Exception innerException) : base(message, innerException) { Errors = []; }

    /// <summary>
    ///     Initializes a new instance of the RequestValidationException class
    /// </summary>
    /// <param name="errors">Validation errors</param>
    public RequestValidationException(IEnumerable<ValidationError> errors) : base("One or more validation failures occurred.") { Errors = errors.ToList().AsReadOnly(); }

    /// <summary>
    ///     Validation errors
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
}
