namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// Simplified student information to avoid circular references
/// </summary>
public class StudentSummaryDto {
  public Guid Id { get; set; }

  public string UserDisplayName { get; set; } = string.Empty;

  public string UserEmail { get; set; } = string.Empty;
}
