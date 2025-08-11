namespace GameGuild.Modules.Programs;

/// <summary>
/// Input types for ActivityGrade GraphQL operations
/// </summary>
public record CreateActivityGradeInput(
  Guid ContentInteractionId,
  Guid GraderProgramUserId,
  decimal Grade,
  string? Feedback = null,
  string? GradingDetails = null
) {
  public Guid ContentInteractionId { get; init; } = ContentInteractionId;

  public Guid GraderProgramUserId { get; init; } = GraderProgramUserId;

  public decimal Grade { get; init; } = Grade;

  public string? Feedback { get; init; } = Feedback;

  public string? GradingDetails { get; init; } = GradingDetails;
}
