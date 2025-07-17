using System.Diagnostics.CodeAnalysis;


namespace GameGuild.Common;

public class Result {
  public Result(bool isSuccess, ErrorMessage error) {
    if ((isSuccess && error != ErrorMessage.None) || (!isSuccess && error == ErrorMessage.None)) throw new ArgumentException("Invalid error", nameof(error));

    IsSuccess = isSuccess;
    ErrorMessage = error;
  }

  public bool IsSuccess { get; }

  public bool IsFailure {
    get => !IsSuccess;
  }

  public ErrorMessage ErrorMessage { get; }

  public static Result Success() { return new Result(true, ErrorMessage.None); }

  public static Result<TValue> Success<TValue>(TValue value) { return new Result<TValue>(value, true, ErrorMessage.None); }

  public static Result Failure(ErrorMessage error) { return new Result(false, error); }

  public static Result<TValue> Failure<TValue>(ErrorMessage error) { return new Result<TValue>(default(TValue?), false, error); }
}

public class Result<TValue>(TValue? value, bool isSuccess, ErrorMessage error) : Result(isSuccess, error) {
  [NotNull]
  public TValue Value {
    get =>
      IsSuccess
        ? value!
        : throw new InvalidOperationException("The value of a failure result can't be accessed.");
  }

  public static implicit operator Result<TValue>(TValue? value) { return value is not null ? Success(value) : Failure<TValue>(ErrorMessage.NullValue); }

  public static Result<TValue> ValidationFailure(ErrorMessage error) { return new Result<TValue>(default(TValue?), false, error); }
}
