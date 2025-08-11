namespace GameGuild.Modules.Programs;

/// <summary>
/// Simplified content interaction information to avoid circular references
/// </summary>
public class ContentInteractionSummaryDto {
  public Guid Id { get; set; }

  public Guid ProgramUserId { get; set; }

  public Guid ContentId { get; set; }

  public string Status { get; set; } = string.Empty;

  public DateTime? SubmittedAt { get; set; }

  public ContentSummaryDto? Content { get; set; }

  public StudentSummaryDto? Student { get; set; }
}
