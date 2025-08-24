namespace GameGuild.Modules.Programs;

/// <summary>
/// DTO for updating existing activity grades
/// </summary>
public record UpdateActivityGradeDto(
  [Range(0, 100)] decimal? Grade = null,
  string? Feedback = null,
  string? GradingDetails = null
) {
  public decimal? Grade { get; init; } = Grade;

  public string? Feedback { get; init; } = Feedback;

  public string? GradingDetails { get; init; } = GradingDetails;
}
