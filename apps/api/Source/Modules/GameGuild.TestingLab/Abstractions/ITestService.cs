
namespace GameGuild.TestingLab;

/// <summary>
/// Composite service interface for Testing operations.
/// Inherits from focused sub-interfaces for backward compatibility with GraphQL and other consumers.
/// New controllers should inject the specific sub-interface they need instead.
/// </summary>
public interface ITestService
    : ITestingRequestOperations,
      ITestingSessionOperations,
      ITestingParticipantOperations,
      ITestingFeedbackOperations,
      ITestingLocationOperations
{
}
