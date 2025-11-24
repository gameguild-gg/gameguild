namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Adapter service that implements ITestingSessionService using ITestService
/// </summary>
public class TestingSessionService : ITestingSessionService
{
    private readonly ITestService _testService;

    public TestingSessionService(ITestService testService)
    {
        _testService = testService;
    }

    public async Task<IEnumerable<TestingSession>> GetAllAsync()
    {
        return await _testService.GetAllTestingSessionsAsync();
    }

    public async Task<IEnumerable<TestingSession>> GetWithPaginationAsync(int skip = 0, int take = 50)
    {
        return await _testService.GetTestingSessionsAsync(skip, take);
    }

    public async Task<TestingSession?> GetByIdAsync(Guid id)
    {
        return await _testService.GetTestingSessionByIdAsync(id);
    }

    public async Task<TestingSession?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _testService.GetTestingSessionByIdWithDetailsAsync(id);
    }

    public async Task<TestingSession> CreateAsync(TestingSession testingSession)
    {
        return await _testService.CreateTestingSessionAsync(testingSession);
    }

    public async Task<TestingSession> UpdateAsync(TestingSession testingSession)
    {
        return await _testService.UpdateTestingSessionAsync(testingSession);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _testService.DeleteTestingSessionAsync(id);
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        return await _testService.RestoreTestingSessionAsync(id);
    }

    public async Task<IEnumerable<TestingSession>> GetByTestingRequestAsync(Guid testingRequestId)
    {
        return await _testService.GetTestingSessionsByRequestAsync(testingRequestId);
    }

    public async Task<IEnumerable<TestingSession>> GetByLocationAsync(Guid locationId)
    {
        return await _testService.GetTestingSessionsByLocationAsync(locationId);
    }

    public async Task<IEnumerable<TestingSession>> GetByStatusAsync(SessionStatus status)
    {
        return await _testService.GetTestingSessionsByStatusAsync(status);
    }

    public async Task<IEnumerable<TestingSession>> GetByManagerAsync(Guid managerId)
    {
        return await _testService.GetTestingSessionsByManagerAsync(managerId);
    }

    public async Task<IEnumerable<TestingSession>> SearchAsync(string searchTerm)
    {
        return await _testService.SearchTestingSessionsAsync(searchTerm);
    }

    public async Task<IEnumerable<TestingSession>> GetUpcomingSessionsAsync()
    {
        // Implementation would need to be added to ITestService or implemented here
        var allSessions = await _testService.GetAllTestingSessionsAsync();
        return allSessions.Where(s => s.StartTime > DateTime.UtcNow);
    }

    public async Task<IEnumerable<TestingSession>> GetActiveSessionsAsync()
    {
        // Implementation would need to be added to ITestService or implemented here
        var allSessions = await _testService.GetAllTestingSessionsAsync();
        return allSessions.Where(s => s.Status == SessionStatus.Active);
    }

    public async Task<IEnumerable<TestingSession>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        // Implementation would need to be added to ITestService or implemented here
        var allSessions = await _testService.GetAllTestingSessionsAsync();
        return allSessions.Where(s => s.StartTime >= startDate && s.StartTime <= endDate);
    }

    public async Task<bool> CanUserJoinSessionAsync(Guid userId, Guid testingSessionId)
    {
        // Implementation would need to be added to ITestService or implemented here
        return true; // Placeholder implementation
    }

    public async Task<TestingSession> JoinSessionAsync(Guid userId, Guid testingSessionId)
    {
        // Implementation would need to be added to ITestService or implemented here
        var session = await GetByIdAsync(testingSessionId);
        return session ?? throw new InvalidOperationException("Testing session not found");
    }

    public async Task<TestingSession> LeaveSessionAsync(Guid userId, Guid testingSessionId)
    {
        // Implementation would need to be added to ITestService or implemented here
        var session = await GetByIdAsync(testingSessionId);
        return session ?? throw new InvalidOperationException("Testing session not found");
    }

    public async Task<TestingSession> StartSessionAsync(Guid testingSessionId)
    {
        var session = await GetByIdAsync(testingSessionId);
        if (session != null)
        {
            session.Status = SessionStatus.Active;
            return await UpdateAsync(session);
        }
        throw new InvalidOperationException("Testing session not found");
    }

    public async Task<TestingSession> EndSessionAsync(Guid testingSessionId)
    {
        var session = await GetByIdAsync(testingSessionId);
        if (session != null)
        {
            session.Status = SessionStatus.Completed;
            return await UpdateAsync(session);
        }
        throw new InvalidOperationException("Testing session not found");
    }

    public async Task<TestingSession> CancelSessionAsync(Guid testingSessionId)
    {
        var session = await GetByIdAsync(testingSessionId);
        if (session != null)
        {
            session.Status = SessionStatus.Cancelled;
            return await UpdateAsync(session);
        }
        throw new InvalidOperationException("Testing session not found");
    }
}