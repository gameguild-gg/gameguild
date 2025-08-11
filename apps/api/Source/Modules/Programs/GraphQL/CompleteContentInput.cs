namespace GameGuild.Modules.Programs;

public record CompleteContentInput(
  Guid InteractionId
) {
  public Guid InteractionId { get; init; } = InteractionId;
}
