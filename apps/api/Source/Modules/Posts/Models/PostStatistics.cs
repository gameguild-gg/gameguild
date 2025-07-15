using GameGuild.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Modules.Posts;

/// <summary>
/// Statistics and metrics for a post
/// </summary>
[Table("post_statistics")]
public class PostStatistics : Entity
{
    /// <summary>
    /// The post this statistics belongs to
    /// </summary>
    public Guid PostId { get; set; }
    public virtual Post Post { get; set; } = null!;

    /// <summary>
    /// Number of unique views
    /// </summary>
    public int ViewsCount { get; set; } = 0;

    /// <summary>
    /// Number of unique viewers
    /// </summary>
    public int UniqueViewersCount { get; set; } = 0;

    /// <summary>
    /// Number of times shared externally
    /// </summary>
    public int ExternalSharesCount { get; set; } = 0;

    /// <summary>
    /// Average engagement time in seconds
    /// </summary>
    public double AverageEngagementTime { get; set; } = 0;

    /// <summary>
    /// Engagement score (calculated field)
    /// </summary>
    public double EngagementScore { get; set; } = 0;

    /// <summary>
    /// Trending score based on recent activity
    /// </summary>
    public double TrendingScore { get; set; } = 0;

    /// <summary>
    /// Last time statistics were updated
    /// </summary>
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
