namespace GameGuild.CQRS;

/// <summary>
/// Represents an error that occurred during an operation
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

    private Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>
    /// The error code
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Creates a new error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance</returns>
    public static Error Create(string code, string message)
    {
        return new Error(code, message);
    }

    /// <summary>
    /// Creates a validation error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance representing a validation failure</returns>
    public static Error Validation(string code, string message)
    {
        return new Error($"Validation.{code}", message);
    }

    /// <summary>
    /// Creates a not found error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance representing a not found failure</returns>
    public static Error NotFound(string code, string message)
    {
        return new Error($"NotFound.{code}", message);
    }

    /// <summary>
    /// Creates a conflict error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance representing a conflict failure</returns>
    public static Error Conflict(string code, string message)
    {
        return new Error($"Conflict.{code}", message);
    }

    /// <summary>
    /// Creates an unauthorized error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance representing an unauthorized failure</returns>
    public static Error Unauthorized(string code, string message)
    {
        return new Error($"Unauthorized.{code}", message);
    }

    /// <summary>
    /// Creates a forbidden error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance representing a forbidden failure</returns>
    public static Error Forbidden(string code, string message)
    {
        return new Error($"Forbidden.{code}", message);
    }

    public override string ToString() => $"{Code}: {Message}";

    public static implicit operator string(Error error) => error.Code;
}
