using System.Diagnostics.CodeAnalysis;

namespace GameGuild;

/// <summary>
/// Simplified Result pattern that uses only Error for all failure scenarios.
/// No more ValidationError, BusinessRuleError, etc. - just Error.
/// </summary>
public class Result : IResult {
  public Result(bool isSuccess, Error error) {
    if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
      throw new ArgumentException("Invalid error", nameof(error));

    IsSuccess = isSuccess;
    Error = error;
  }

  public bool IsSuccess { get; }
  public bool IsFailure => !IsSuccess;
  public Error Error { get; }

  // Factory methods
  public static Result Success() => new(true, Error.None);
  public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
  public static Result Failure(Error error) => new(false, error);
  public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

  // Convenience methods for common failure scenarios
  public static Result ValidationFailure(string propertyName, string message, object? attemptedValue = null)
    => Failure(Error.ValidationFailure(propertyName, message, attemptedValue));

  public static Result<TValue> ValidationFailure<TValue>(string propertyName, string message, object? attemptedValue = null)
    => Failure<TValue>(Error.ValidationFailure(propertyName, message, attemptedValue));

  public static Result BusinessRuleViolation(string rule, string message, object? context = null)
    => Failure(Error.BusinessRule(rule, message, context));

  public static Result<TValue> BusinessRuleViolation<TValue>(string rule, string message, object? context = null)
    => Failure<TValue>(Error.BusinessRule(rule, message, context));

  public static Result NotFound(string resource, object? identifier = null)
    => Failure(Error.NotFound($"{resource}.NotFound", $"{resource} not found" + (identifier != null ? $": {identifier}" : "")));

  public static Result<TValue> NotFound<TValue>(string resource, object? identifier = null)
    => Failure<TValue>(Error.NotFound($"{resource}.NotFound", $"{resource} not found" + (identifier != null ? $": {identifier}" : "")));
}

/// <summary>
/// Result with a value - simplified to only use Error
/// </summary>
public class Result<TValue> : Result {
  private readonly TValue? _value;

  public Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) {
    _value = value;
  }

  [NotNull]
  public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("The value of a failure result can't be accessed.");

  // Implicit conversions
  public static implicit operator Result<TValue>(TValue? value)
    => value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

  public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

  // Functional methods
  public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper) {
    return IsFailure ? Failure<TNew>(Error) : Success(mapper(Value));
  }

  public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> binder) {
    return IsFailure ? Failure<TNew>(Error) : binder(Value);
  }

  public async Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> binder) {
    return IsFailure ? Failure<TNew>(Error) : await binder(Value);
  }
}
