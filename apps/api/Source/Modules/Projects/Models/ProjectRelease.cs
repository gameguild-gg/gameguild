using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Represents a release/version of a project
/// </summary>
[Table("ProjectReleases")]
[Index(nameof(ProjectId), nameof(ReleaseVersion), IsUnique = true, Name = "IX_ProjectReleases_Project_Version")]
[Index(nameof(ProjectId), nameof(ReleasedAt), Name = "IX_ProjectReleases_Project_Date")]
[Index(nameof(IsLatest), Name = "IX_ProjectReleases_Latest")]
public class ProjectRelease : Resource {
  /// <summary>
  /// Project this release belongs to
  /// </summary>
  public Guid ProjectId { get; set; }

  // Navigation property commented out temporarily to resolve circular compilation
  // /// <summary>
  // /// Navigation property to project
  // /// </summary>
  // public virtual Project Project { get; set; } = null!;

  /// <summary>
  /// Release version string (e.g., "1.0.0", "2.1.3-beta")
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string ReleaseVersion { get; set; } = string.Empty;

  // Title and Description are inherited from ResourceBase
  // We can override them if needed with different attributes

  /// <summary>
  /// Date when this version was released
  /// </summary>
  public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Whether this is the latest release
  /// </summary>
  public bool IsLatest { get; set; } = false;

  /// <summary>
  /// Whether this is a pre-release (alpha, beta, etc.)
  /// </summary>
  public bool IsPrerelease { get; set; } = false;

  /// <summary>
  /// Download URL for this release
  /// </summary>
  [MaxLength(500)]
  public string? DownloadUrl { get; set; }

  /// <summary>
  /// File size in bytes
  /// </summary>
  public long? FileSize { get; set; }

  /// <summary>
  /// Number of downloads for this release
  /// </summary>
  public int DownloadCount { get; set; } = 0;

  /// <summary>
  /// Release notes in markdown format
  /// </summary>
  public string? ReleaseNotes { get; set; }

  /// <summary>
  /// Checksum/hash of the release file
  /// </summary>
  [MaxLength(128)]
  public string? Checksum { get; set; }

  /// <summary>
  /// Minimum system requirements
  /// </summary>
  [MaxLength(1000)]
  public string? SystemRequirements { get; set; }

  /// <summary>
  /// Supported platforms (JSON array)
  /// </summary>
  [MaxLength(500)]
  public string? SupportedPlatforms { get; set; }

  /// <summary>
  /// Release type (stable, beta, alpha, etc.)
  /// </summary>
  [MaxLength(50)]
  public string ReleaseType { get; set; } = "stable";

  /// <summary>
  /// Release status
  /// </summary>
  public ContentStatus Status { get; set; } = ContentStatus.Draft;

  /// <summary>
  /// Build number or commit hash
  /// </summary>
  [MaxLength(100)]
  public string? BuildNumber { get; set; }

  /// <summary>
  /// Additional release metadata (JSON)
  /// </summary>
  [MaxLength(2000)]
  public string? ReleaseMetadata { get; set; }
}
