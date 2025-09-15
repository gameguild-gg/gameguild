namespace GameGuild.CQRS;

/// <summary>
/// Base interface for all operation results
/// </summary>
public interface IResult
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    bool IsFailure => !IsSuccess;

    /// <summary>
    /// The error that occurred during the operation (if any)
    /// </summary>
    Error? Error { get; }

    /// <summary>
    /// Collection of validation errors (if any)
    /// </summary>
    IEnumerable<ValidationError> ValidationErrors { get; }
}

/// <summary>
/// Generic interface for operation results with a value
/// </summary>
/// <typeparam name="TValue">The type of the value</typeparam>
public interface IResult<out TValue> : IResult
{
    /// <summary>
    /// The value returned by the operation (if successful)
    /// </summary>
    TValue? Value { get; }
}
