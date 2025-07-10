namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// Simplified grader information to avoid circular references
/// </summary>
public class GraderSummaryDto {
  public Guid Id { get; set; }

  public string UserDisplayName { get; set; } = string.Empty;

  public string UserEmail { get; set; } = string.Empty;

  public string Role { get; set; } = string.Empty;
}
