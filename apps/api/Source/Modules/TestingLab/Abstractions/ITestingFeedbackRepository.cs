namespace GameGuild.Modules.TestingLab;

/// <summary> Repository abstraction for Testing Feedback operations </summary>
public interface ITestingFeedbackRepository {
  /// <summary> Get feedback with pagination </summary>
  Task<IEnumerable<TestingFeedback>> GetWithPaginationAsync(int skip = 0, int take = 50);

  /// <summary> Get feedback by testing request </summary>
  Task<IEnumerable<TestingFeedback>> GetByTestingRequestAsync(Guid testingRequestId);

  /// <summary> Get feedback by testing session </summary>
  Task<IEnumerable<TestingFeedback>> GetByTestingSessionAsync(Guid testingSessionId);

  /// <summary> Get feedback by tester </summary>
  Task<IEnumerable<TestingFeedback>> GetByTesterAsync(Guid testerId);

  /// <summary> Get feedback by quality rating </summary>
  Task<IEnumerable<TestingFeedback>> GetByQualityRatingAsync(FeedbackQuality rating);

  /// <summary> Get feedback statistics for a testing request </summary>
  Task<TestingFeedbackStats> GetFeedbackStatsAsync(Guid testingRequestId);
}
