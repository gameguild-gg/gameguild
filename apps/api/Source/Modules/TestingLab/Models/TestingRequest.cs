using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;
using GameGuild.Modules.Project.Models;


namespace GameGuild.Modules.TestingLab.Models {
  public class TestingRequest : BaseEntity {
    /// <summary>
    /// Foreign key to the project version
    /// </summary>
    public Guid ProjectVersionId { get; set; }

    /// <summary>
    /// Navigation property to the project version
    /// </summary>
    public virtual ProjectVersion ProjectVersion { get; set; } = null!;

    [Required][MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public InstructionType InstructionsType { get; set; }

    public string? InstructionsContent { get; set; }

    [MaxLength(500)]
    public string? InstructionsUrl { get; set; }

    public Guid? InstructionsFileId { get; set; }

    public int? MaxTesters { get; set; }

    public int CurrentTesterCount { get; set; } = 0;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public TestingRequestStatus Status { get; set; } = TestingRequestStatus.Draft;

    /// <summary>
    /// Foreign key to the user who created this request
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Navigation property to the user who created this request
    /// </summary>
    public virtual Modules.User.Models.User CreatedBy { get; set; } = null!;
  }
}
