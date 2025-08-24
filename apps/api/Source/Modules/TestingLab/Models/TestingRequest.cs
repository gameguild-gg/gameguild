using GameGuild.Common;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab {
  public class TestingRequest : Entity {
    /// <summary>
    /// Foreign key to the project version
    /// </summary>
    public Guid ProjectVersionId { get; set; }

    /// <summary>
    /// Navigation property to the project version
    /// </summary>
    public virtual ProjectVersion ProjectVersion { get; set; } = null!;

    [Required][MaxLength(255)] public string Title { get; set; } = string.Empty;

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

    public int CurrentTesterCount { get; set; } = 0;

    [Required] public DateTime StartDate { get; set; }

    [Required] public DateTime EndDate { get; set; }

    [Required] public TestingRequestStatus Status { get; set; } = TestingRequestStatus.Draft;

    /// <summary>
    /// Foreign key to the user who created this request
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Navigation property to the user who created this request
    /// </summary>
    public virtual User CreatedBy { get; set; } = null!;
  }
}
