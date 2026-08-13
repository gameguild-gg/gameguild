using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Users;

namespace GameGuild.Projects;

/// <summary> Represents a version/release of a project </summary>
[Table("ProjectVersions")]
[Index(nameof(ProjectId))]
[Index(nameof(VersionNumber))]
[Index(nameof(CreatedById))]
public class ProjectVersion : EntityBase<Guid>
{
    /// <summary> The project this version belongs to </summary>
    [Required]
    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;

    public Guid ProjectId { get; set; }

    /// <summary> Version number (e.g., "1.0.0", "alpha-0.1") </summary>
    [Required]
    [MaxLength(50)]
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary> Release notes </summary>
    public string? ReleaseNotes { get; set; }

    /// <summary> Status (enum as string) </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    /// <summary> Download count </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary> User who created this version </summary>
    [Required]
    [JsonIgnore]
    public virtual User CreatedBy { get; set; } = null!;

    public Guid CreatedById { get; set; }
}
