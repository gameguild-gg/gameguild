namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Repository interface for managing testing locations
/// </summary>
public interface ITestingLocationRepository
{
    Task<TestingLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TestingLocation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TestingLocation>> GetByStatusAsync(LocationStatus status, CancellationToken cancellationToken = default);
    Task<TestingLocation> CreateAsync(TestingLocation location, CancellationToken cancellationToken = default);
    Task<TestingLocation> UpdateAsync(TestingLocation location, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
