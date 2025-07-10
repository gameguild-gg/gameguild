namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// Statistics type for content interactions
/// </summary>
public record ContentInteractionStats {
  public Guid ProgramId { get; init; }

  public int TotalInteractions { get; init; }

  public int CompletedInteractions { get; init; }

  public int SubmittedInteractions { get; init; }

  public int InProgressInteractions { get; init; }

  public decimal AverageCompletionPercentage { get; init; }

  public decimal AverageTimeSpentMinutes { get; init; }
}
