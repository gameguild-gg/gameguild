using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.TestingLab {
  public class CreateTestingRequestDto {
    [Required] public Guid ProjectVersionId { get; set; }

    [Required] [MaxLength(255)] public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// URL to download the game build
    /// </summary>
    [MaxLength(1000)]
    public string? DownloadUrl { get; set; }

    [Required] public InstructionType InstructionsType { get; set; }

    public string? InstructionsContent { get; set; }

    [MaxLength(500)] public string? InstructionsUrl { get; set; }

    public Guid? InstructionsFileId { get; set; }

    /// <summary>
    /// Simple feedback form content (plain text questions)
    /// </summary>
    public string? FeedbackFormContent { get; set; }

    public int? MaxTesters { get; set; }

    [Required] public DateTime StartDate { get; set; }

    [Required] public DateTime EndDate { get; set; }

    [Required] public TestingRequestStatus Status { get; set; }

    public TestingRequest ToTestingRequest(Guid createdById) {
      return new TestingRequest {
        ProjectVersionId = ProjectVersionId,
        Title = Title,
        Description = Description,
        DownloadUrl = DownloadUrl,
        InstructionsType = InstructionsType,
        InstructionsContent = InstructionsContent,
        InstructionsUrl = InstructionsUrl,
        InstructionsFileId = InstructionsFileId,
        FeedbackFormContent = FeedbackFormContent,
        MaxTesters = MaxTesters,
        StartDate = StartDate,
        EndDate = EndDate,
        Status = Status,
        CreatedById = createdById,
      };
    }
  }
}
