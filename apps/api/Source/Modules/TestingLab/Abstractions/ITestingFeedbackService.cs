namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary> Service abstraction for Testing Feedback operations </summary>
public interface ITestingFeedbackService {
  Task<IEnumerable<TestingFeedback>> GetAllAsync();

  Task<IEnumerable<TestingFeedback>> GetWithPaginationAsync(int skip = 0, int take = 50);

  Task<TestingFeedback?> GetByIdAsync(Guid id);

  Task<TestingFeedback> CreateAsync(TestingFeedback testingFeedback);

  Task<TestingFeedback> UpdateAsync(TestingFeedback testingFeedback);

  Task<bool> DeleteAsync(Guid id);

  // Specialized operations
  Task<IEnumerable<TestingFeedback>> GetByTestingRequestAsync(Guid testingRequestId);

  Task<IEnumerable<TestingFeedback>> GetByTestingSessionAsync(Guid testingSessionId);

  Task<IEnumerable<TestingFeedback>> GetByTesterAsync(Guid testerId);

  Task<TestingFeedbackStats> GetFeedbackStatsAsync(Guid testingRequestId);

  Task<bool> CanUserSubmitFeedbackAsync(Guid userId, Guid testingRequestId);

  Task<TestingFeedback> SubmitFeedbackAsync(Guid userId, Guid testingRequestId, string content, FeedbackQualityRating? qualityRating = null);

  Task<TestingFeedback> RateFeedbackQualityAsync(Guid feedbackId, FeedbackQualityRating rating);
}
