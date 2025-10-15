using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Represents a version/release of a project
/// </summary>
public class ProjectVersion : Entity {
  /// <summary>
  /// The project this version belongs to
  /// </summary>
  [Required]
  public virtual Project Project { get; set; } = null!;

  public Guid ProjectId { get; set; }

  /// <summary>
  /// Version number (e.g., "1.0.0", "alpha-0.1")
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string VersionNumber { get; set; } = string.Empty;

  /// <summary>
  /// Release notes
  /// </summary>
  public string? ReleaseNotes { get; set; }

  /// <summary>
  /// Status (enum as string)
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Status { get; set; } = "draft";

  /// <summary>
  /// Download count
  /// </summary>
  public int DownloadCount { get; set; } = 0;

  /// <summary>
  /// User who created this version
  /// </summary>
  [Required]
  public virtual User CreatedBy { get; set; } = null!;

  public Guid CreatedById { get; set; }
}
