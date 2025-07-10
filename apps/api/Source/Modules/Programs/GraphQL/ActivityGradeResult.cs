using GameGuild.Modules.Programs.Models;


namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// Result types for ActivityGrade operations
/// </summary>
public class ActivityGradeResult {
  public bool Success { get; set; }

  public string? ErrorMessage { get; set; }

  public ActivityGrade? Grade { get; set; }
}
