namespace GameGuild.Modules.Programs;

/// <summary>
/// DTO for grade statistics responses
/// </summary>
public class GradeStatisticsDto {
  public int TotalGrades { get; set; }

  public decimal AverageGrade { get; set; }

  public decimal MinGrade { get; set; }

  public decimal MaxGrade { get; set; }

  public decimal PassingRate { get; set; }

  // Additional computed properties for better UX
  public string AverageGradeFormatted {
    get => $"{AverageGrade:F1}%";
  }

  public string PassingRateFormatted {
    get => $"{PassingRate:F1}%";
  }

  public bool HasGrades {
    get => TotalGrades > 0;
  }
}
