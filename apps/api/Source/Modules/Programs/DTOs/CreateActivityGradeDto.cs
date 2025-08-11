using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Programs;

/// <summary>
/// DTO for creating new activity grades
/// </summary>
public record CreateActivityGradeDto(
  [Required] Guid ContentInteractionId,
  [Required] Guid GraderProgramUserId,
  [Required] [Range(0, 100)] decimal Grade,
  string? Feedback = null,
  string? GradingDetails = null
) {
  public Guid ContentInteractionId { get; init; } = ContentInteractionId;

  public Guid GraderProgramUserId { get; init; } = GraderProgramUserId;

  public decimal Grade { get; init; } = Grade;

  public string? Feedback { get; init; } = Feedback;

  public string? GradingDetails { get; init; } = GradingDetails;
}
