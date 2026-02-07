namespace GameGuild.Models;

/// <summary>
///     Validation exception for domain validation errors associated with the Models.Result pattern.
///     For CQRS pipeline validation exceptions, use <see cref="GameGuild.CQRS.ValidationException" />.
/// </summary>
/// <remarks>
///     These two exception types serve different purposes:
///     - <see cref="GameGuild.CQRS.ValidationException" /> is thrown by the CQRS validation pipeline behavior
///       and carries <see cref="GameGuild.CQRS.ValidationError" /> items.
///     - This type is for domain-level validation in the Result pattern, carrying an <see cref="Error" />.
/// </remarks>
public class ValidationException : Exception
{
    public ValidationException() : base("Validation failed") { }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, Exception innerException) : base(message, innerException) { }

    public ValidationException(Error error) : base(error.Description) { Error = error; }

    /// <summary>The domain error that caused validation to fail, if provided.</summary>
    public Error? Error { get; }
}
