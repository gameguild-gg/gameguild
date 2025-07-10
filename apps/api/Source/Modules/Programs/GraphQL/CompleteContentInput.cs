namespace GameGuild.Modules.Programs.GraphQL;

public record CompleteContentInput(
  Guid InteractionId
) {
  public Guid InteractionId { get; init; } = InteractionId;
}
