namespace GameGuild.Modules.Programs;

public record SubmitContentInput(
  Guid InteractionId,
  string SubmissionData
) {
  public Guid InteractionId { get; init; } = InteractionId;

  public string SubmissionData { get; init; } = SubmissionData;
}
