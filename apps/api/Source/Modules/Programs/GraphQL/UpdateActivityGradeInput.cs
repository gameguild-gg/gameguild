namespace GameGuild.Modules.Programs.GraphQL;

public record UpdateActivityGradeInput(
  Guid GradeId,
  decimal? Grade = null,
  string? Feedback = null,
  string? GradingDetails = null
) {
  public Guid GradeId { get; init; } = GradeId;

  public decimal? Grade { get; init; } = Grade;

  public string? Feedback { get; init; } = Feedback;

  public string? GradingDetails { get; init; } = GradingDetails;
}
