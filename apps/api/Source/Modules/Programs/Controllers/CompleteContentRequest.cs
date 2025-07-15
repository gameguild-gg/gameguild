namespace GameGuild.Modules.Programs;

public record CompleteContentRequest(Guid ProgramUserId, Guid ContentId) {
  public Guid ProgramUserId { get; init; } = ProgramUserId;

  public Guid ContentId { get; init; } = ContentId;
}
