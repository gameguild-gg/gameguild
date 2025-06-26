using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a version/release of a project
/// </summary>
public class ProjectVersion : BaseEntity
{
    private Project _project = null!;

    private Guid _projectId;

    private string _versionNumber = string.Empty;

    private string? _releaseNotes;

    private string _status = "draft";

    private int _downloadCount = 0;

    private User.Models.User _createdBy = null!;

    private Guid _createdById;

    /// <summary>
    /// The project this version belongs to
    /// </summary>
    [Required]
    public virtual Project Project
    {
        get => _project;
        set => _project = value;
    }

    public Guid ProjectId
    {
        get => _projectId;
        set => _projectId = value;
    }

    /// <summary>
    /// Version number (e.g., "1.0.0", "alpha-0.1")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string VersionNumber
    {
        get => _versionNumber;
        set => _versionNumber = value;
    }

    /// <summary>
    /// Release notes
    /// </summary>
    public string? ReleaseNotes
    {
        get => _releaseNotes;
        set => _releaseNotes = value;
    }

    /// <summary>
    /// Status (enum as string)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status
    {
        get => _status;
        set => _status = value;
    }

    /// <summary>
    /// Download count
    /// </summary>
    public int DownloadCount
    {
        get => _downloadCount;
        set => _downloadCount = value;
    }

    /// <summary>
    /// User who created this version
    /// </summary>
    [Required]
    public virtual User.Models.User CreatedBy
    {
        get => _createdBy;
        set => _createdBy = value;
    }

    public Guid CreatedById
    {
        get => _createdById;
        set => _createdById = value;
    }
}
