namespace GameGuild.Modules.Programs;

/// <summary>
/// Result types for ActivityGrade operations
/// </summary>
public class ActivityGradeResult {
  public bool Success { get; set; }

  public string? Error { get; set; }

  public ActivityGrade? Grade { get; set; }
}
