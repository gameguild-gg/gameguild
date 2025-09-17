namespace GameGuild.CQRS;

/// <summary>
/// Represents the result of an operation that can either succeed or fail
/// </summary>
public class Result : IResult
{
    protected Result(bool isSuccess, Error? error, IEnumerable<ValidationError>? validationErrors = null)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Successful result cannot have an error");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failed result must have an error");

        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors ?? [];
    }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    /// <inheritdoc />
    public Error? Error { get; }

    /// <inheritdoc />
    public IEnumerable<ValidationError> ValidationErrors { get; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success()
    {
        return new Result(true, Error.None);
    }

    /// <summary>
    /// Creates a failed result with an error
    /// </summary>
    /// <param name="error">The error that occurred</param>
    /// <returns>A failed result</returns>
    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="validationErrors">The validation errors that occurred</param>
    /// <returns>A failed result</returns>
    public static Result ValidationFailure(IEnumerable<ValidationError> validationErrors)
    {
        return new Result(false, Error.Validation("ValidationFailed", "One or more validation errors occurred"), validationErrors);
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="validationErrors">The validation errors that occurred</param>
    /// <returns>A failed result</returns>
    public static Result ValidationFailure(params ValidationError[] validationErrors)
    {
        return ValidationFailure(validationErrors.AsEnumerable());
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="value">The value</param>
    /// <returns>A successful result with a value</returns>
    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new Result<TValue>(value, true, Error.None);
    }

    /// <summary>
    /// Creates a failed result with an error
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="error">The error that occurred</param>
    /// <returns>A failed result</returns>
    public static Result<TValue> Failure<TValue>(Error error)
    {
        return new Result<TValue>(default, false, error);
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="validationErrors">The validation errors that occurred</param>
    /// <returns>A failed result</returns>
    public static Result<TValue> ValidationFailure<TValue>(IEnumerable<ValidationError> validationErrors)
    {
        return new Result<TValue>(default, false, Error.Validation("ValidationFailed", "One or more validation errors occurred"), validationErrors);
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="validationErrors">The validation errors that occurred</param>
    /// <returns>A failed result</returns>
    public static Result<TValue> ValidationFailure<TValue>(params ValidationError[] validationErrors)
    {
        return ValidationFailure<TValue>(validationErrors.AsEnumerable());
    }
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail
/// </summary>
/// <typeparam name="TValue">The type of the value</typeparam>
public class Result<TValue> : Result, IResult<TValue>
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error? error, IEnumerable<ValidationError>? validationErrors = null)
        : base(isSuccess, error, validationErrors)
    {
        _value = value;
    }

    /// <inheritdoc />
    public TValue? Value => IsSuccess ? _value : default;

    /// <summary>
    /// Implicitly converts a value to a successful result
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>A successful result with the value</returns>
    public static implicit operator Result<TValue>(TValue? value)
    {
        return value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
    }

    /// <summary>
    /// Implicitly converts an error to a failed result
    /// </summary>
    /// <param name="error">The error</param>
    /// <returns>A failed result with the error</returns>
    public static implicit operator Result<TValue>(Error error)
    {
        return Failure<TValue>(error);
    }

    /// <summary>
    /// Maps the value of a successful result to a new type
    /// </summary>
    /// <typeparam name="TNew">The new type</typeparam>
    /// <param name="mapper">Function to map the value</param>
    /// <returns>A result with the mapped value</returns>
    public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper)
    {
        if (IsFailure)
            return Failure<TNew>(Error!);

        return Success(mapper(Value!));
    }

    /// <summary>
    /// Binds the result to another operation that returns a result
    /// </summary>
    /// <typeparam name="TNew">The new type</typeparam>
    /// <param name="binder">Function that returns a new result</param>
    /// <returns>The result of the binding operation</returns>
    public async Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> binder)
    {
        if (IsFailure)
            return Failure<TNew>(Error!);

        return await binder(Value!);
    }

    /// <summary>
    /// Binds the result to another operation that returns a result
    /// </summary>
    /// <typeparam name="TNew">The new type</typeparam>
    /// <param name="binder">Function that returns a new result</param>
    /// <returns>The result of the binding operation</returns>
    public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> binder)
    {
        if (IsFailure)
            return Failure<TNew>(Error!);

        return binder(Value!);
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result<TValue> OnSuccess(Action<TValue> action)
    {
        if (IsSuccess)
            action(Value!);

        return this;
    }

    /// <summary>
    /// Executes an action if the result is failed
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result<TValue> OnFailure(Action<Error> action)
    {
        if (IsFailure)
            action(Error!);

        return this;
    }

    /// <summary>
    /// Matches the result and executes the appropriate action
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="onSuccess">Action to execute if successful</param>
    /// <param name="onFailure">Action to execute if failed</param>
    /// <returns>The result of the matching action</returns>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}
