namespace GameGuild.Common;

public record ErrorMessage(string Code, string Description, ErrorType Type) {
  public static readonly ErrorMessage None = new(string.Empty, string.Empty, ErrorType.Failure);

  public static readonly ErrorMessage NullValue = new(
    "General.Null",
    "Null value was provided",
    ErrorType.Failure
  );

  public string Code { get; } = Code;

  public string Description { get; } = Description;

  public ErrorType Type { get; } = Type;

  public static ErrorMessage Failure(string code, string description) { return new ErrorMessage(code, description, ErrorType.Failure); }

  public static ErrorMessage PageNotFound(string code, string description) { return new ErrorMessage(code, description, ErrorType.PageNotFound); }

  public static ErrorMessage Problem(string code, string description) { return new ErrorMessage(code, description, ErrorType.Problem); }

  public static ErrorMessage Conflict(string code, string description) { return new ErrorMessage(code, description, ErrorType.Conflict); }
}
