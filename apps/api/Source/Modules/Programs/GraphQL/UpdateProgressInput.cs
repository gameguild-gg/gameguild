namespace GameGuild.Modules.Programs;

public record UpdateProgressInput(
  Guid InteractionId,
  decimal CompletionPercentage
) {
  public Guid InteractionId { get; init; } = InteractionId;

  public decimal CompletionPercentage { get; init; } = CompletionPercentage;
}
