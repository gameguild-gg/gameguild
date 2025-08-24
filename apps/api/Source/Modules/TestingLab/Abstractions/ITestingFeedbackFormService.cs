namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Service interface for managing testing feedback operations
/// </summary>
public interface ITestingFeedbackFormService
{
    Task<TestingFeedbackForm?> GetFeedbackFormAsync(Guid testingRequestId, CancellationToken cancellationToken = default);
    Task<TestingFeedbackForm> CreateFeedbackFormAsync(Guid testingRequestId, string content, CancellationToken cancellationToken = default);
    Task<TestingFeedbackForm> UpdateFeedbackFormAsync(Guid id, string content, CancellationToken cancellationToken = default);
    Task<bool> DeleteFeedbackFormAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ValidateFeedbackResponseAsync(Guid formId, string response, CancellationToken cancellationToken = default);
}
