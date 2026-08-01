namespace GameGuild.TestingLab;

/// <summary>
/// Service interface for testing location operations.
/// Extracted from ITestService for focused responsibility.
/// </summary>
public interface ITestingLocationOperations
{
    Task<IEnumerable<TestingLocation>> GetAllTestingLocationsAsync();
    Task<IEnumerable<TestingLocation>> GetTestingLocationsAsync(int skip = 0, int take = 50, bool includeArchived = false);
    Task<TestingLocation?> GetTestingLocationByIdAsync(Guid id);
    Task<TestingLocation> CreateTestingLocationAsync(TestingLocation location);
    Task<TestingLocation> UpdateTestingLocationAsync(TestingLocation location);
    Task<bool> DeleteTestingLocationAsync(Guid id);
    Task<bool> RestoreTestingLocationAsync(Guid id);
}
