namespace GameGuild.Modules.Programs.Controllers;

public record SubmitContentRequest(Guid ProgramUserId, Guid ContentId, string SubmissionData) {
  public Guid ProgramUserId { get; init; } = ProgramUserId;

  public Guid ContentId { get; init; } = ContentId;

  public string SubmissionData { get; init; } = SubmissionData;
}
