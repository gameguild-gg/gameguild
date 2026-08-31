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
    public string VersionNumber { get; private set; } = string.Empty;

    /// <summary> Release notes </summary>
    public string? ReleaseNotes { get; private set; }

    /// <summary> Release lifecycle status </summary>
    [Required]
    public ProjectVersionStatus Status { get; private set; } = ProjectVersionStatus.Draft;

    /// <summary> Download count </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary> User who created this version </summary>
    [Required]
    [JsonIgnore]
    public virtual User CreatedBy { get; set; } = null!;

    public Guid CreatedById { get; set; }

    public static ProjectVersion Create(
        Guid projectId,
        string versionNumber,
        string? releaseNotes,
        Guid createdById,
        Guid? tenantId)
    {
        if (projectId == Guid.Empty) throw new ArgumentException("Project is required.", nameof(projectId));
        if (createdById == Guid.Empty) throw new ArgumentException("Creator is required.", nameof(createdById));
        if (string.IsNullOrWhiteSpace(versionNumber))
            throw new ArgumentException("Version number is required.", nameof(versionNumber));

        return new ProjectVersion
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            VersionNumber = versionNumber.Trim(),
            ReleaseNotes = NormalizeNotes(releaseNotes),
            CreatedById = createdById,
            Status = ProjectVersionStatus.Draft,
        };
    }

    public void UpdateDraft(string versionNumber, string? releaseNotes)
    {
        RequireStatus(ProjectVersionStatus.Draft);
        if (string.IsNullOrWhiteSpace(versionNumber))
            throw new ArgumentException("Version number is required.", nameof(versionNumber));
        VersionNumber = versionNumber.Trim();
        ReleaseNotes = NormalizeNotes(releaseNotes);
        Touch();
    }

    public void MarkReadyForTesting()
    {
        RequireStatus(ProjectVersionStatus.Draft);
        Status = ProjectVersionStatus.ReadyForTesting;
        Touch();
    }

    public void Release()
    {
        RequireStatus(ProjectVersionStatus.ReadyForTesting);
        Status = ProjectVersionStatus.Released;
        Touch();
    }

    public void Archive()
    {
        if (Status is not (ProjectVersionStatus.ReadyForTesting or ProjectVersionStatus.Released))
            throw new InvalidOperationException("Only ready or released versions can be archived.");
        Status = ProjectVersionStatus.Archived;
        Touch();
    }

    private static string? NormalizeNotes(string? releaseNotes) =>
        string.IsNullOrWhiteSpace(releaseNotes) ? null : releaseNotes.Trim();

    private void RequireStatus(ProjectVersionStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Expected version status {expected}, but was {Status}.");
    }
}
