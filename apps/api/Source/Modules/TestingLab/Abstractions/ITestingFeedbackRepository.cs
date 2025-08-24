using GameGuild.Common;

namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Repository abstraction for Testing Feedback operations
/// </summary>
public interface ITestingFeedbackRepository : IRepository<TestingFeedback>
{
    /// <summary>
    /// Get feedback with pagination
    /// </summary>
    Task<IEnumerable<TestingFeedback>> GetWithPaginationAsync(int skip = 0, int take = 50);

    /// <summary>
    /// Get feedback by testing request
    /// </summary>
    Task<IEnumerable<TestingFeedback>> GetByTestingRequestAsync(Guid testingRequestId);

    /// <summary>
    /// Get feedback by testing session
    /// </summary>
    Task<IEnumerable<TestingFeedback>> GetByTestingSessionAsync(Guid testingSessionId);

    /// <summary>
    /// Get feedback by tester
    /// </summary>
    Task<IEnumerable<TestingFeedback>> GetByTesterAsync(Guid testerId);

    /// <summary>
    /// Get feedback by quality rating
    /// </summary>
    Task<IEnumerable<TestingFeedback>> GetByQualityRatingAsync(FeedbackQualityRating rating);

    /// <summary>
    /// Get feedback statistics for a testing request
    /// </summary>
    Task<TestingFeedbackStats> GetFeedbackStatsAsync(Guid testingRequestId);
}

/// <summary>
/// Statistics for testing feedback
/// </summary>
public class TestingFeedbackStats
{
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<FeedbackQualityRating, int> RatingDistribution { get; set; } = new();
    public int HighQualityCount { get; set; }
    public int MediumQualityCount { get; set; }
    public int LowQualityCount { get; set; }
}
