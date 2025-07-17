namespace GameGuild.Common;

public sealed record ValidationError(ErrorMessage[] Errors) : ErrorMessage(
  "Validation.General",
  "One or more validation errors occurred",
  ErrorType.Validation
) {
  public static ValidationError FromResults(IEnumerable<Result> results) { return new ValidationError(results.Where(r => r.IsFailure).Select(r => r.ErrorMessage).ToArray()); }
}
