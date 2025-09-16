namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Repository implementation for TestingLocation using ITestService
/// </summary>
public class TestingLocationRepository : ITestingLocationRepository
{
    private readonly ITestService _testService;

    public TestingLocationRepository(ITestService testService)
    {
        _testService = testService;
    }

    public async Task<TestingLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _testService.GetTestingLocationByIdAsync(id);
    }

    public async Task<IEnumerable<TestingLocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _testService.GetTestingLocationsAsync();
    }

    public async Task<IEnumerable<TestingLocation>> GetByStatusAsync(LocationStatus status, CancellationToken cancellationToken = default)
    {
        // Implementation would need to be added to ITestService or implemented here
        var allLocations = await _testService.GetTestingLocationsAsync();
        return allLocations.Where(l => l.Status == status);
    }

    public async Task<TestingLocation> CreateAsync(TestingLocation location, CancellationToken cancellationToken = default)
    {
        return await _testService.CreateTestingLocationAsync(location);
    }

    public async Task<TestingLocation> UpdateAsync(TestingLocation location, CancellationToken cancellationToken = default)
    {
        return await _testService.UpdateTestingLocationAsync(location);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _testService.DeleteTestingLocationAsync(id);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await _testService.GetTestingLocationByIdAsync(id);
        return location != null;
    }
}