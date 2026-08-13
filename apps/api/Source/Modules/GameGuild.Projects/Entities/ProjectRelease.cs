using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects;

/// <summary> Represents a release/version of a project </summary>
[Table("ProjectReleases")]
[Index(nameof(ProjectId), nameof(ReleaseVersion), IsUnique = true, Name = "IX_ProjectReleases_Project_Version")]
[Index(nameof(ProjectId), nameof(ReleasedAt), Name = "IX_ProjectReleases_Project_Date")]
[Index(nameof(IsLatest), Name = "IX_ProjectReleases_Latest")]
public class ProjectRelease : EntityBase<Guid>
{
    /// <summary> Project this release belongs to </summary>
    public Guid ProjectId { get; set; }

    /// <summary> Navigation property to project </summary>
    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;

    /// <summary> Release title </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary> Release description </summary>
    public string? Description { get; set; }

    /// <summary> Release version string (e.g., "1.0.0", "2.1.3-beta") </summary>
    [Required]
    [MaxLength(50)]
    public string ReleaseVersion { get; set; } = string.Empty;

    /// <summary> Date when this version was released </summary>
    public DateTime ReleasedAt { get; set; } = SystemClock.UtcNow;

    /// <summary> Whether this is the latest release </summary>
    public bool IsLatest { get; set; } = false;

    /// <summary> Whether this is a pre-release (alpha, beta, etc.) </summary>
    public bool IsPrerelease { get; set; } = false;

    /// <summary> Download URL for this release </summary>
    [MaxLength(500)]
    public string? DownloadUrl { get; set; }

    /// <summary> File size in bytes </summary>
    public long? FileSize { get; set; }

    /// <summary> Number of downloads for this release </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary> Release notes in markdown format </summary>
    public string? ReleaseNotes { get; set; }

    /// <summary> Checksum/hash of the release file </summary>
    [MaxLength(128)]
    public string? Checksum { get; set; }

    /// <summary> Minimum system requirements </summary>
    [MaxLength(1000)]
    public string? SystemRequirements { get; set; }

    /// <summary> Supported platforms (JSON array) </summary>
    [MaxLength(500)]
    public string? SupportedPlatforms { get; set; }

    /// <summary> Release type (stable, beta, alpha, etc.) </summary>
    [MaxLength(50)]
    public string ReleaseType { get; set; } = "stable";

    /// <summary> Release status </summary>
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    /// <summary> Build number or commit hash </summary>
    [MaxLength(100)]
    public string? BuildNumber { get; set; }

    /// <summary> Additional release metadata (JSON) </summary>
    [MaxLength(2000)]
    public string? ReleaseMetadata { get; set; }
}
