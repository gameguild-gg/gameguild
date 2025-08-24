namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary> Service abstraction for Testing Session operations </summary>
public interface ITestingSessionService {
  Task<IEnumerable<TestingSession>> GetAllAsync();

  Task<IEnumerable<TestingSession>> GetWithPaginationAsync(int skip = 0, int take = 50);

  Task<TestingSession?> GetByIdAsync(Guid id);

  Task<TestingSession?> GetByIdWithDetailsAsync(Guid id);

  Task<TestingSession> CreateAsync(TestingSession testingSession);

  Task<TestingSession> UpdateAsync(TestingSession testingSession);

  Task<bool> DeleteAsync(Guid id);

  Task<bool> RestoreAsync(Guid id);

  // Specialized operations
  Task<IEnumerable<TestingSession>> GetByTestingRequestAsync(Guid testingRequestId);

  Task<IEnumerable<TestingSession>> GetByStatusAsync(SessionStatus status);

  Task<IEnumerable<TestingSession>> GetUpcomingSessionsAsync();

  Task<IEnumerable<TestingSession>> GetActiveSessionsAsync();

  Task<IEnumerable<TestingSession>> GetByLocationAsync(Guid locationId);

  Task<IEnumerable<TestingSession>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

  Task<bool> CanUserJoinSessionAsync(Guid userId, Guid testingSessionId);

  Task<TestingSession> JoinSessionAsync(Guid userId, Guid testingSessionId);

  Task<TestingSession> LeaveSessionAsync(Guid userId, Guid testingSessionId);

  Task<TestingSession> StartSessionAsync(Guid testingSessionId);

  Task<TestingSession> EndSessionAsync(Guid testingSessionId);

  Task<TestingSession> CancelSessionAsync(Guid testingSessionId);
}
