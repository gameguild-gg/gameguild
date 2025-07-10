namespace GameGuild.Modules.Programs.Services;

/// <summary>
/// Grade statistics for reporting
/// </summary>
public class GradeStatistics {
  public int TotalGrades { get; set; }

  public decimal AverageGrade { get; set; }

  public decimal MinGrade { get; set; }

  public decimal MaxGrade { get; set; }

  public decimal PassingRate { get; set; }
}
