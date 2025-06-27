using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a release/version of a project
/// </summary>
[Table("ProjectReleases")]
[Index(nameof(ProjectId), nameof(ReleaseVersion), IsUnique = true, Name = "IX_ProjectReleases_Project_Version")]
[Index(nameof(ProjectId), nameof(ReleasedAt), Name = "IX_ProjectReleases_Project_Date")]
[Index(nameof(IsLatest), Name = "IX_ProjectReleases_Latest")]
public class ProjectRelease : ResourceBase {
  private Guid _projectId;

  private string _releaseVersion = string.Empty;

  private DateTime _releasedAt = DateTime.UtcNow;

  private bool _isLatest = false;

  private bool _isPrerelease = false;

  private string? _downloadUrl;

  private long? _fileSize;

  private int _downloadCount = 0;

  private string? _releaseNotes;

  private string? _checksum;

  private string? _systemRequirements;

  private string? _supportedPlatforms;

  private string _releaseType = "stable";

  private ContentStatus _status = ContentStatus.Draft;

  private string? _buildNumber;

  private string? _releaseMetadata;

  /// <summary>
  /// Project this release belongs to
  /// </summary>
  public Guid ProjectId {
    get => _projectId;
    set => _projectId = value;
  }

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
  public string ReleaseVersion {
    get => _releaseVersion;
    set => _releaseVersion = value;
  }

  // Title and Description are inherited from ResourceBase
  // We can override them if needed with different attributes

  /// <summary>
  /// Date when this version was released
  /// </summary>
  public DateTime ReleasedAt {
    get => _releasedAt;
    set => _releasedAt = value;
  }

  /// <summary>
  /// Whether this is the latest release
  /// </summary>
  public bool IsLatest {
    get => _isLatest;
    set => _isLatest = value;
  }

  /// <summary>
  /// Whether this is a pre-release (alpha, beta, etc.)
  /// </summary>
  public bool IsPrerelease {
    get => _isPrerelease;
    set => _isPrerelease = value;
  }

  /// <summary>
  /// Download URL for this release
  /// </summary>
  [MaxLength(500)]
  public string? DownloadUrl {
    get => _downloadUrl;
    set => _downloadUrl = value;
  }

  /// <summary>
  /// File size in bytes
  /// </summary>
  public long? FileSize {
    get => _fileSize;
    set => _fileSize = value;
  }

  /// <summary>
  /// Number of downloads for this release
  /// </summary>
  public int DownloadCount {
    get => _downloadCount;
    set => _downloadCount = value;
  }

  /// <summary>
  /// Release notes in markdown format
  /// </summary>
  public string? ReleaseNotes {
    get => _releaseNotes;
    set => _releaseNotes = value;
  }

  /// <summary>
  /// Checksum/hash of the release file
  /// </summary>
  [MaxLength(128)]
  public string? Checksum {
    get => _checksum;
    set => _checksum = value;
  }

  /// <summary>
  /// Minimum system requirements
  /// </summary>
  [MaxLength(1000)]
  public string? SystemRequirements {
    get => _systemRequirements;
    set => _systemRequirements = value;
  }

  /// <summary>
  /// Supported platforms (JSON array)
  /// </summary>
  [MaxLength(500)]
  public string? SupportedPlatforms {
    get => _supportedPlatforms;
    set => _supportedPlatforms = value;
  }

  /// <summary>
  /// Release type (stable, beta, alpha, etc.)
  /// </summary>
  [MaxLength(50)]
  public string ReleaseType {
    get => _releaseType;
    set => _releaseType = value;
  }

  /// <summary>
  /// Release status
  /// </summary>
  public ContentStatus Status {
    get => _status;
    set => _status = value;
  }

  /// <summary>
  /// Build number or commit hash
  /// </summary>
  [MaxLength(100)]
  public string? BuildNumber {
    get => _buildNumber;
    set => _buildNumber = value;
  }

  /// <summary>
  /// Additional release metadata (JSON)
  /// </summary>
  [MaxLength(2000)]
  public string? ReleaseMetadata {
    get => _releaseMetadata;
    set => _releaseMetadata = value;
  }
}
