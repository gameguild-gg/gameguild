using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents feedback/review for a project
/// </summary>
[Table("ProjectFeedbacks")]
[Index(nameof(ProjectId), nameof(UserId), IsUnique = true, Name = "IX_ProjectFeedbacks_Project_User")]
[Index(nameof(ProjectId), nameof(Rating), Name = "IX_ProjectFeedbacks_Project_Rating")]
[Index(nameof(UserId), Name = "IX_ProjectFeedbacks_User")]
[Index(nameof(CreatedAt), Name = "IX_ProjectFeedbacks_Date")]
public class ProjectFeedback : ResourceBase
{
    /// <summary>
    /// Project being reviewed
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to project
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// User providing feedback
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User.Models.User User { get; set; } = null!;

    /// <summary>
    /// Rating (1-5 stars)
    /// </summary>
    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    /// <summary>
    /// Review title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public new string Title { get; set; } = string.Empty;

    /// <summary>
    /// Review content
    /// </summary>
    [MaxLength(2000)]
    public string? Content { get; set; }

    /// <summary>
    /// Feedback categories (JSON array)
    /// </summary>
    [MaxLength(500)]
    public string? Categories { get; set; }

    /// <summary>
    /// Whether this feedback is featured
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Whether this feedback has been verified (e.g., from actual user)
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Feedback status
    /// </summary>
    public ContentStatus Status { get; set; } = ContentStatus.Published;

    /// <summary>
    /// Number of helpful votes
    /// </summary>
    public int HelpfulVotes { get; set; } = 0;

    /// <summary>
    /// Number of total votes
    /// </summary>
    public int TotalVotes { get; set; } = 0;

    /// <summary>
    /// Platform where the project was experienced (if applicable)
    /// </summary>
    [MaxLength(100)]
    public string? Platform { get; set; }

    /// <summary>
    /// Version of the project this feedback is for
    /// </summary>
    [MaxLength(50)]
    public string? ProjectVersion { get; set; }
}
