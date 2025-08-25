namespace GameGuild.Modules.TestingLab;

/// <summary> Repository abstraction for Testing Request operations </summary>
public interface ITestingRequestRepository {
  /// <summary> Get testing requests with pagination </summary>
  Task<IEnumerable<TestingRequest>> GetWithPaginationAsync(int skip = 0, int take = 50);

  /// <summary> Get a testing request by ID with all related details </summary>
  Task<TestingRequest?> GetByIdWithDetailsAsync(Guid id);

  /// <summary> Get testing requests by project version </summary>
  Task<IEnumerable<TestingRequest>> GetByProjectVersionAsync(Guid projectVersionId);

  /// <summary> Get testing requests by status </summary>
  Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status);

  /// <summary> Get testing requests by status with pagination </summary>
  Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status, int skip = 0, int take = 50);

  /// <summary> Get active testing requests that are currently accepting testers </summary>
  Task<IEnumerable<TestingRequest>> GetActiveRequestsAsync();

  /// <summary> Get testing requests that need closing (passed end date) </summary>
  Task<IEnumerable<TestingRequest>> GetRequestsNeedingClosureAsync();

  /// <summary> Get testing requests created by a specific user </summary>
  Task<IEnumerable<TestingRequest>> GetByCreatedByAsync(Guid userId);

  /// <summary> Search testing requests by title or description </summary>
  Task<IEnumerable<TestingRequest>> SearchAsync(string searchTerm);
}
