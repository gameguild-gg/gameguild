using GameGuild.CQRS;

namespace GameGuild;

public sealed record ValidationError(GameGuild.Error[] Errors) : GameGuild.Error(
  "Validation.General",
  "One or more validation errors occurred",
  ErrorType.Validation
) {
  public static ValidationError FromResults(IEnumerable<GameGuild.CQRS.Result> results) {
    return new ValidationError(results
      .Where(r => r.IsFailure && r.Error != null)
      .Select(r => new GameGuild.Error(r.Error!.Code, r.Error.Message, ErrorType.Validation))
      .ToArray());
  }
}
