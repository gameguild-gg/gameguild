namespace GameGuild;

public sealed record ValidationError(Error[ ] Errors) : Error("Validation.General", "One or more validation errors occurred", ErrorType.Validation) {
  public static ValidationError FromResults(IEnumerable<CQRS.Result> results) {
    return new ValidationError(results.Where(r => r.IsFailure && r.Error != null).Select(r => new Error(r.Error!.Code, r.Error.Message, ErrorType.Validation)).ToArray());
  }
}
