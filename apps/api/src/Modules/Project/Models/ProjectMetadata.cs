using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Stores statistics and metadata for a project.
/// </summary>
public class ProjectMetadata
{
    private Guid _id;

    private Project _project = null!;

    private Guid _projectId;

    private int _viewCount = 0;

    private int _downloadCount = 0;

    private int _followerCount = 0;

    [Key]
    public Guid Id
    {
        get => _id;
        set => _id = value;
    }

    /// <summary>
    /// Navigation property to the project
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
    /// Project statistics
    /// </summary>
    public int ViewCount
    {
        get => _viewCount;
        set => _viewCount = value;
    }

    public int DownloadCount
    {
        get => _downloadCount;
        set => _downloadCount = value;
    }

    public int FollowerCount
    {
        get => _followerCount;
        set => _followerCount = value;
    }
}
