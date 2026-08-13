namespace GameGuild.TestingLab;

/// <summary>
/// Service interface for feedback, statistics, and reporting operations.
/// Extracted from ITestService for focused responsibility.
/// </summary>
public interface ITestingFeedbackOperations
{
    // Feedback CRUD
    Task<TestingFeedback> AddFeedbackAsync(Guid testingRequestId, Guid userId, Guid feedbackFormId, string feedbackData, TestingContext context, Guid? sessionId = null, string? additionalNotes = null);
    Task<IEnumerable<TestingFeedback>> GetTestingRequestFeedbackAsync(Guid testingRequestId);
    Task<IEnumerable<TestingFeedback>> GetFeedbackByUserAsync(Guid userId);
    Task<TestingFeedbackDirectoryPage> GetFeedbackDirectoryAsync(
        TestingFeedbackDirectoryQuery query,
        CancellationToken cancellationToken = default);

    // Simplified feedback
    Task SubmitFeedbackAsync(SubmitFeedbackDto feedbackDto, Guid userId);

    // Statistics
    Task<object> GetTestingRequestStatisticsAsync(Guid testingRequestId);

    // Feedback reporting & quality
    Task ReportFeedbackAsync(Guid feedbackId, string reason, Guid reportedByUserId);
    Task RateFeedbackQualityAsync(Guid feedbackId, FeedbackQuality quality, Guid ratedByUserId);
}
