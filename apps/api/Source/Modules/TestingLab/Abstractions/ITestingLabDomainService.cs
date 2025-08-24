namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary> Domain service for coordinating complex Testing Lab operations </summary>
public interface ITestingLabDomainService {
  /// <summary> Orchestrates the complete process of creating a testing session with participants </summary>
  Task<TestingSession> CreateTestingSessionWithParticipantsAsync(
    CreateTestingSessionCommand command,
    IEnumerable<Guid> participantIds,
    CancellationToken cancellationToken = default
  );

  /// <summary> Processes bulk feedback submission for a testing request </summary>
  Task<IEnumerable<TestingFeedback>> ProcessBulkFeedbackAsync(
    Guid testingRequestId,
    IEnumerable<SubmitFeedbackCommand> feedbackCommands,
    CancellationToken cancellationToken = default
  );

  /// <summary> Automatically closes expired testing requests and creates summary reports </summary>
  Task<IEnumerable<TestingRequest>> ProcessExpiredTestingRequestsAsync(
    CancellationToken cancellationToken = default
  );

  /// <summary> Validates if a user can participate in a specific testing request </summary>
  Task<(bool CanParticipate, string? Reason)> ValidateUserParticipationAsync(
    Guid userId,
    Guid testingRequestId,
    CancellationToken cancellationToken = default
  );

  /// <summary> Generates comprehensive testing statistics and analytics </summary>
  Task<TestingAnalytics> GenerateTestingAnalyticsAsync(
    Guid? projectVersionId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    CancellationToken cancellationToken = default
  );
}
