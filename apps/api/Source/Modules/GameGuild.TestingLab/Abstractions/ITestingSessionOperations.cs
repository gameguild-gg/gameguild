namespace GameGuild.TestingLab;

/// <summary>
/// Service interface for testing session operations.
/// Extracted from ITestService for focused responsibility.
/// </summary>
public interface ITestingSessionOperations
{
    // CRUD
    Task<IEnumerable<TestingSession>> GetAllTestingSessionsAsync();
    Task<IEnumerable<TestingSession>> GetTestingSessionsAsync(int skip = 0, int take = 50);
    Task<TestingSession?> GetTestingSessionByIdAsync(Guid id);
    Task<TestingSession?> GetTestingSessionByIdWithDetailsAsync(Guid id);
    Task<TestingSession> CreateTestingSessionAsync(TestingSession testingSession);
    Task<TestingSession> UpdateTestingSessionAsync(TestingSession testingSession);
    Task<bool> DeleteTestingSessionAsync(Guid id);
    Task<bool> RestoreTestingSessionAsync(Guid id);

    // Filtered queries
    Task<IEnumerable<TestingSession>> GetTestingSessionsByRequestAsync(Guid testingRequestId);
    Task<IEnumerable<TestingSession>> GetTestingSessionsByLocationAsync(Guid locationId);
    Task<IEnumerable<TestingSession>> GetTestingSessionsByStatusAsync(SessionStatus status);
    Task<IEnumerable<TestingSession>> GetTestingSessionsByManagerAsync(Guid managerId);
    Task<IEnumerable<TestingSession>> SearchTestingSessionsAsync(string searchTerm);
    Task<IEnumerable<TestingSession>> GetPublicTestingSessionsAsync(int take = 100);

    // Statistics
    Task<object> GetTestingSessionStatisticsAsync(Guid testingSessionId);

    // Attendance
    Task<object> GetSessionAttendanceReportAsync();
    Task UpdateSessionAttendanceAsync(Guid sessionId, Guid userId, AttendanceStatus status, Guid updatedByUserId);
}
