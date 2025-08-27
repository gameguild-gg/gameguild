namespace GameGuild.Modules.TestingLab;

/// <summary> Represents comprehensive analytics data for testing lab operations </summary>
public class TestingAnalytics {
  public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

  public DateTime? FromDate { get; set; }

  public DateTime? ToDate { get; set; }

  public Guid? ProjectVersionId { get; set; }

  // Request Statistics
  public int TotalRequests { get; set; }

  public int ActiveRequests { get; set; }

  public int CompletedRequests { get; set; }

  public int CancelledRequests { get; set; }

  // Session Statistics
  public int TotalSessions { get; set; }

  public int CompletedSessions { get; set; }

  public int CancelledSessions { get; set; }

  public int UpcomingSessions { get; set; }

  // Participant Statistics
  public int TotalParticipants { get; set; }

  public int ActiveParticipants { get; set; }

  public double AverageParticipantsPerSession { get; set; }

  // Feedback Statistics
  public int TotalFeedbackSubmitted { get; set; }

  public double AverageFeedbackRating { get; set; }

  public Dictionary<FeedbackQuality, int> FeedbackQualityDistribution { get; set; } = new Dictionary<FeedbackQuality, int>();

  // Time-based Statistics
  public double AverageSessionDurationMinutes { get; set; }

  public double AverageTimeBetweenRequestAndFirstSession { get; set; }

  public double AverageTimeToCompleteTesting { get; set; }

  // Engagement Metrics
  public double ParticipantRetentionRate { get; set; }

  public double FeedbackCompletionRate { get; set; }

  public int MostPopularTestingMode { get; set; } // TestingMode enum value

  // Location Statistics (for in-person testing)
  public Dictionary<Guid, int> LocationUsageStatistics { get; set; } = new Dictionary<Guid, int>();

  // Trend Data
  public List<DailyTestingMetrics> DailyMetrics { get; set; } = new List<DailyTestingMetrics>();
}

public class DailyTestingMetrics {
  public DateTime Date { get; set; }

  public int NewRequests { get; set; }

  public int CompletedSessions { get; set; }

  public int FeedbackSubmitted { get; set; }

  public int ActiveParticipants { get; set; }
}
