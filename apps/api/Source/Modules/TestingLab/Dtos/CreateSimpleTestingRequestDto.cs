namespace GameGuild.Modules.TestingLab;

/// <summary> Simplified DTO for students to submit versions directly without pre-existing ProjectVersion </summary>
public class CreateSimpleTestingRequestDto {
  [Required][MaxLength(255)] public string Title { get; set; } = string.Empty;

  public string? Description { get; set; }

  /// <summary> Version number for this submission </summary>
  [Required][MaxLength(50)] public string VersionNumber { get; set; } = string.Empty;

  /// <summary> URL to download the game build </summary>
  [MaxLength(1000)]
  public string? DownloadUrl { get; set; }

  [Required] public InstructionType InstructionsType { get; set; }

  public string? InstructionsContent { get; set; }

  [MaxLength(500)] public string? InstructionsUrl { get; set; }

  /// <summary> Simple feedback form content (plain text questions) </summary>
  public string? FeedbackFormContent { get; set; }

  public int? MaxTesters { get; set; }

  public DateTime? StartDate { get; set; }

  public DateTime? EndDate { get; set; }

  /// <summary> Team identifier (e.g., fa23-capstone-2023-24-t01) </summary>
  [Required][MaxLength(100)] public string TeamIdentifier { get; set; } = string.Empty;
}
