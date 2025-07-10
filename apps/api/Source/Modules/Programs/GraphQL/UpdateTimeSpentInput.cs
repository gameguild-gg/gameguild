namespace GameGuild.Modules.Programs.GraphQL;

public record UpdateTimeSpentInput(
  Guid InteractionId,
  int AdditionalMinutes
) {
  public Guid InteractionId { get; init; } = InteractionId;

  public int AdditionalMinutes { get; init; } = AdditionalMinutes;
}
