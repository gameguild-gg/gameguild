namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Adapter service that implements ITestingRequestService using ITestService
/// </summary>
public class TestingRequestService : ITestingRequestService
{
    private readonly ITestService _testService;

    public TestingRequestService(ITestService testService)
    {
        _testService = testService;
    }

    public async Task<IEnumerable<TestingRequest>> GetAllAsync()
    {
        return await _testService.GetAllTestingRequestsAsync();
    }

    public async Task<IEnumerable<TestingRequest>> GetWithPaginationAsync(int skip = 0, int take = 50)
    {
        return await _testService.GetTestingRequestsAsync(skip, take);
    }

    public async Task<TestingRequest?> GetByIdAsync(Guid id)
    {
        return await _testService.GetTestingRequestByIdAsync(id);
    }

    public async Task<TestingRequest?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _testService.GetTestingRequestByIdWithDetailsAsync(id);
    }

    public async Task<TestingRequest> CreateAsync(TestingRequest testingRequest)
    {
        return await _testService.CreateTestingRequestAsync(testingRequest);
    }

    public async Task<TestingRequest> UpdateAsync(TestingRequest testingRequest)
    {
        return await _testService.UpdateTestingRequestAsync(testingRequest);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _testService.DeleteTestingRequestAsync(id);
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        return await _testService.RestoreTestingRequestAsync(id);
    }

    public async Task<IEnumerable<TestingRequest>> GetByProjectVersionAsync(Guid projectVersionId)
    {
        return await _testService.GetTestingRequestsByProjectVersionAsync(projectVersionId);
    }

    public async Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status)
    {
        return await _testService.GetTestingRequestsByStatusAsync(status);
    }

    public async Task<IEnumerable<TestingRequest>> GetActiveRequestsAsync()
    {
        // Implementation would need to be added to ITestService or implemented here
        var allRequests = await _testService.GetAllTestingRequestsAsync();
        return allRequests.Where(r => r.Status == TestingRequestStatus.Open);
    }

    public async Task<IEnumerable<TestingRequest>> GetRequestsNeedingClosureAsync()
    {
        // Implementation would need to be added to ITestService or implemented here
        var allRequests = await _testService.GetAllTestingRequestsAsync();
        return allRequests.Where(r => r.EndDate < DateTime.UtcNow && r.Status == TestingRequestStatus.Open);
    }

    public async Task<bool> CanUserJoinTestingAsync(Guid userId, Guid testingRequestId)
    {
        // Implementation would need to be added to ITestService or implemented here
        var isParticipant = await _testService.IsUserParticipantAsync(testingRequestId, userId);
        return !isParticipant;
    }

    public async Task<TestingRequest> JoinTestingAsync(Guid userId, Guid testingRequestId)
    {
        await _testService.AddParticipantAsync(testingRequestId, userId);
        return await GetByIdAsync(testingRequestId) ?? throw new InvalidOperationException("Testing request not found");
    }

    public async Task<TestingRequest> LeaveTestingAsync(Guid userId, Guid testingRequestId)
    {
        await _testService.RemoveParticipantAsync(testingRequestId, userId);
        return await GetByIdAsync(testingRequestId) ?? throw new InvalidOperationException("Testing request not found");
    }

    public async Task<TestingRequest> CloseTestingRequestAsync(Guid testingRequestId)
    {
        var request = await GetByIdAsync(testingRequestId);
        if (request != null)
        {
            request.Status = TestingRequestStatus.Completed;
            return await UpdateAsync(request);
        }
        throw new InvalidOperationException("Testing request not found");
    }

    public async Task<IEnumerable<TestingRequest>> SearchAsync(string searchTerm)
    {
        return await _testService.SearchTestingRequestsAsync(searchTerm);
    }
}