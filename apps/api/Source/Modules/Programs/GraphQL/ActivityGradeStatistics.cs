namespace GameGuild.Modules.Programs;

public class ActivityGradeStatistics {
  public int TotalGrades { get; set; }

  public decimal AverageGrade { get; set; }

  public decimal MinGrade { get; set; }

  public decimal MaxGrade { get; set; }

  public decimal PassingRate { get; set; }

  public bool HasGrades {
    get => TotalGrades > 0;
  }

  public string AverageGradeFormatted {
    get => $"{AverageGrade:F1}%";
  }

  public string PassingRateFormatted {
    get => $"{PassingRate:F1}%";
  }
}
