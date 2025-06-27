namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Statistics and analytics data for a project
/// </summary>
public class ProjectStatistics {
  private Guid _projectId;

  private int _followerCount;

  private int _feedbackCount;

  private decimal? _averageRating;

  private int _totalDownloads;

  private int _activeTeamCount;

  private int _collaboratorCount;

  private int _releaseCount;

  private int _jamSubmissionCount;

  private int _awardCount;

  private int _viewsLast30Days;

  private int _downloadsLast30Days;

  private int _newFollowersLast30Days;

  private DateTime _calculatedAt = DateTime.UtcNow;

  private decimal _trendingScore;

  private int? _popularityRank;

  /// <summary>
  /// Project ID
  /// </summary>
  public Guid ProjectId {
    get => _projectId;
    set => _projectId = value;
  }

  /// <summary>
  /// Total number of followers
  /// </summary>
  public int FollowerCount {
    get => _followerCount;
    set => _followerCount = value;
  }

  /// <summary>
  /// Total number of feedback/reviews
  /// </summary>
  public int FeedbackCount {
    get => _feedbackCount;
    set => _feedbackCount = value;
  }

  /// <summary>
  /// Average rating (1-5 stars)
  /// </summary>
  public decimal? AverageRating {
    get => _averageRating;
    set => _averageRating = value;
  }

  /// <summary>
  /// Total number of downloads across all releases
  /// </summary>
  public int TotalDownloads {
    get => _totalDownloads;
    set => _totalDownloads = value;
  }

  /// <summary>
  /// Number of active teams working on the project
  /// </summary>
  public int ActiveTeamCount {
    get => _activeTeamCount;
    set => _activeTeamCount = value;
  }

  /// <summary>
  /// Number of collaborators
  /// </summary>
  public int CollaboratorCount {
    get => _collaboratorCount;
    set => _collaboratorCount = value;
  }

  /// <summary>
  /// Number of releases
  /// </summary>
  public int ReleaseCount {
    get => _releaseCount;
    set => _releaseCount = value;
  }

  /// <summary>
  /// Number of jam submissions
  /// </summary>
  public int JamSubmissionCount {
    get => _jamSubmissionCount;
    set => _jamSubmissionCount = value;
  }

  /// <summary>
  /// Number of awards won in jams
  /// </summary>
  public int AwardCount {
    get => _awardCount;
    set => _awardCount = value;
  }

  /// <summary>
  /// Views/visits in the last 30 days
  /// </summary>
  public int ViewsLast30Days {
    get => _viewsLast30Days;
    set => _viewsLast30Days = value;
  }

  /// <summary>
  /// Downloads in the last 30 days
  /// </summary>
  public int DownloadsLast30Days {
    get => _downloadsLast30Days;
    set => _downloadsLast30Days = value;
  }

  /// <summary>
  /// New followers in the last 30 days
  /// </summary>
  public int NewFollowersLast30Days {
    get => _newFollowersLast30Days;
    set => _newFollowersLast30Days = value;
  }

  /// <summary>
  /// Date when statistics were calculated
  /// </summary>
  public DateTime CalculatedAt {
    get => _calculatedAt;
    set => _calculatedAt = value;
  }

  /// <summary>
  /// Trending score (calculated based on recent activity)
  /// </summary>
  public decimal TrendingScore {
    get => _trendingScore;
    set => _trendingScore = value;
  }

  /// <summary>
  /// Popularity rank among all projects
  /// </summary>
  public int? PopularityRank {
    get => _popularityRank;
    set => _popularityRank = value;
  }
}
