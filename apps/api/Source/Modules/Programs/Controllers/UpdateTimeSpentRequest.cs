namespace GameGuild.Modules.Programs;

public record UpdateTimeSpentRequest(Guid ProgramUserId, Guid ContentId, int AdditionalMinutes) {
  public Guid ProgramUserId { get; init; } = ProgramUserId;

  public Guid ContentId { get; init; } = ContentId;

  public int AdditionalMinutes { get; init; } = AdditionalMinutes;
}
