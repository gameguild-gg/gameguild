namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Repository implementation for TestingRequest using ITestService
/// </summary>
public class TestingRequestRepository : ITestingRequestRepository
{
    private readonly ITestService _testService;

    public TestingRequestRepository(ITestService testService)
    {
        _testService = testService;
    }

    public async Task<IEnumerable<TestingRequest>> GetWithPaginationAsync(int skip = 0, int take = 50)
    {
        return await _testService.GetTestingRequestsAsync(skip, take);
    }

    public async Task<TestingRequest?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _testService.GetTestingRequestByIdWithDetailsAsync(id);
    }

    public async Task<IEnumerable<TestingRequest>> GetByProjectVersionAsync(Guid projectVersionId)
    {
        return await _testService.GetTestingRequestsByProjectVersionAsync(projectVersionId);
    }

    public async Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status)
    {
        return await _testService.GetTestingRequestsByStatusAsync(status);
    }

    public async Task<IEnumerable<TestingRequest>> GetByStatusAsync(TestingRequestStatus status, int skip = 0, int take = 50)
    {
        var allByStatus = await _testService.GetTestingRequestsByStatusAsync(status);
        return allByStatus.Skip(skip).Take(take);
    }

    public async Task<IEnumerable<TestingRequest>> GetActiveRequestsAsync()
    {
        return await GetByStatusAsync(TestingRequestStatus.Open);
    }

    public async Task<IEnumerable<TestingRequest>> GetRequestsNeedingClosureAsync()
    {
        var allRequests = await _testService.GetAllTestingRequestsAsync();
        return allRequests.Where(r => r.EndDate < DateTime.UtcNow && r.Status == TestingRequestStatus.Open);
    }

    public async Task<IEnumerable<TestingRequest>> GetByCreatedByAsync(Guid userId)
    {
        return await _testService.GetTestingRequestsByCreatorAsync(userId);
    }

    public async Task<IEnumerable<TestingRequest>> SearchAsync(string searchTerm)
    {
        return await _testService.SearchTestingRequestsAsync(searchTerm);
    }
}