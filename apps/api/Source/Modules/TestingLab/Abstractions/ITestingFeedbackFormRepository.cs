namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Repository interface for managing testing feedback forms
/// </summary>
public interface ITestingFeedbackFormRepository
{
    Task<TestingFeedbackForm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TestingFeedbackForm?> GetByTestingRequestIdAsync(Guid testingRequestId, CancellationToken cancellationToken = default);
    Task<TestingFeedbackForm> CreateAsync(TestingFeedbackForm form, CancellationToken cancellationToken = default);
    Task<TestingFeedbackForm> UpdateAsync(TestingFeedbackForm form, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
