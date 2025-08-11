namespace GameGuild.Modules.Programs;

/// <summary>
/// Input types for ContentInteraction GraphQL operations
/// </summary>
public record StartContentInput(
  Guid ProgramUserId,
  Guid ContentId
) {
  public Guid ProgramUserId { get; init; } = ProgramUserId;

  public Guid ContentId { get; init; } = ContentId;
}
