using System.ComponentModel.DataAnnotations;
using GameGuild;

namespace GameGuild.Projects.Entities;

/// <summary> Stores statistics and metadata for a project. </summary>
public class ProjectMetadata : EntityBase<Guid>
{
    /// <summary> Navigation property to the project </summary>
    [Required]
    public virtual Project Project { get; set; } = null!;

    public Guid ProjectId { get; set; }

    /// <summary> Project statistics </summary>
    public int ViewCount { get; set; } = 0;

    public int DownloadCount { get; set; } = 0;

    public int FollowerCount { get; set; } = 0;
}
