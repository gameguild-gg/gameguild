namespace GameGuild.Modules.TestingLab;

/// <summary> Statistics for testing feedback </summary>
public class TestingFeedbackStats {
  public int TotalFeedback { get; set; }

  public double AverageRating { get; set; }

  public Dictionary<FeedbackQuality, int> RatingDistribution { get; set; } = new Dictionary<FeedbackQuality, int>();

  public int HighQualityCount { get; set; }

  public int MediumQualityCount { get; set; }

  public int LowQualityCount { get; set; }

  public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
