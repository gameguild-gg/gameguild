namespace GameGuild.Modules.TestingLab;

/// <summary> Service abstraction for Testing Request operations </summary>
public interface ITestingRequestService {
  Task<IEnumerable<TestingRequest>> GetAllAsync();

  Task<IEnumerable<TestingRequest>> GetWithPaginationAsync(int skip = 0, int take = 50);

  Task<TestingRequest?> GetByIdAsync(Guid id);

  Task<TestingRequest?> GetByIdWithDetailsAsync(Guid id);

  Task<TestingRequest> CreateAsync(TestingRequest testingRequest);

  Task<TestingRequest> UpdateAsync(TestingRequest testingRequest);

  Task<bool> DeleteAsync(Guid id);

  Task<bool> RestoreAsync(Guid id);

  // Specialized operations
  Task<IEnumerable<TestingRequest>> GetByProjectVersionAsync(Guid projectVersionId);

  Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status);

  Task<IEnumerable<TestingRequest>> GetActiveRequestsAsync();

  Task<IEnumerable<TestingRequest>> GetRequestsNeedingClosureAsync();

  Task<bool> CanUserJoinTestingAsync(Guid userId, Guid testingRequestId);

  Task<TestingRequest> JoinTestingAsync(Guid userId, Guid testingRequestId);

  Task<TestingRequest> LeaveTestingAsync(Guid userId, Guid testingRequestId);

  Task<TestingRequest> CloseTestingRequestAsync(Guid testingRequestId);

  Task<IEnumerable<TestingRequest>> SearchAsync(string searchTerm);
}
