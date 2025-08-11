namespace GameGuild.Modules.Programs;

/// <summary>
/// Simplified content information to avoid circular references
/// </summary>
public class ContentSummaryDto {
  public Guid Id { get; set; }

  public string Title { get; set; } = string.Empty;

  public string ContentType { get; set; } = string.Empty;

  public int? EstimatedMinutes { get; set; }
}
