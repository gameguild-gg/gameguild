namespace GameGuild.Models;

/// <summary>
///     Validation exception for domain validation errors
/// </summary>
public class ValidationException : Exception
{
    public ValidationException() : base("Validation failed") { }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
