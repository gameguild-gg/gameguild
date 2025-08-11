namespace GameGuild.Modules.Projects;

/// <summary>
/// Statistics and analytics data for a project
/// </summary>
public class ProjectStatistics {
  /// <summary>
  /// Project ID
  /// </summary>
  public Guid ProjectId { get; set; }

  /// <summary>
  /// Total number of followers
  /// </summary>
  public int FollowerCount { get; set; }

  /// <summary>
  /// Total number of feedback/reviews
  /// </summary>
  public int FeedbackCount { get; set; }

  /// <summary>
  /// Average rating (1-5 stars)
  /// </summary>
  public decimal? AverageRating { get; set; }

  /// <summary>
  /// Total number of downloads across all releases
  /// </summary>
  public int TotalDownloads { get; set; }

  /// <summary>
  /// Number of active teams working on the project
  /// </summary>
  public int ActiveTeamCount { get; set; }

  /// <summary>
  /// Number of collaborators
  /// </summary>
  public int CollaboratorCount { get; set; }

  /// <summary>
  /// Number of releases
  /// </summary>
  public int ReleaseCount { get; set; }

  /// <summary>
  /// Number of jam submissions
  /// </summary>
  public int JamSubmissionCount { get; set; }

  /// <summary>
  /// Number of awards won in jams
  /// </summary>
  public int AwardCount { get; set; }

  /// <summary>
  /// Views/visits in the last 30 days
  /// </summary>
  public int ViewsLast30Days { get; set; }

  /// <summary>
  /// Downloads in the last 30 days
  /// </summary>
  public int DownloadsLast30Days { get; set; }

  /// <summary>
  /// New followers in the last 30 days
  /// </summary>
  public int NewFollowersLast30Days { get; set; }

  /// <summary>
  /// Date when statistics were calculated
  /// </summary>
  public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Trending score (calculated based on recent activity)
  /// </summary>
  public decimal TrendingScore { get; set; }

  /// <summary>
  /// Popularity rank among all projects
  /// </summary>
  public int? PopularityRank { get; set; }
}
