namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// DTO for ActivityGrade responses - avoids circular references for Swagger/OpenAPI
/// </summary>
public class ActivityGradeDto {
  public Guid Id { get; set; }

  public Guid ContentInteractionId { get; set; }

  public Guid GraderProgramUserId { get; set; }

  public decimal Grade { get; set; }

  public string? Feedback { get; set; }

  public string? GradingDetails { get; set; }

  public DateTime GradedAt { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  // Simplified nested objects to avoid circular references
  public ContentInteractionSummaryDto? ContentInteraction { get; set; }

  public GraderSummaryDto? Grader { get; set; }

  // Computed properties for convenience
  public bool IsPassingGrade {
    get => Grade >= 70; // Assuming 70% is passing
  }

  public string GradePercentage {
    get => $"{Grade:F1}%";
  }

  public bool HasFeedback {
    get => !string.IsNullOrEmpty(Feedback);
  }

  public bool HasGradingDetails {
    get => !string.IsNullOrEmpty(GradingDetails);
  }
}
