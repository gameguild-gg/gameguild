

namespace GameGuild.TestingLab;

public class TestingLocationRepository : ITestingLocationRepository {
  private readonly IApplicationDbContext _context;

  public TestingLocationRepository(IApplicationDbContext context) { _context = context; }

  public async Task<TestingLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.Set<TestingLocation>().Where(tl => tl.Id == id && tl.DeletedAt == null).FirstOrDefaultAsync(cancellationToken); }

  public async Task<IEnumerable<TestingLocation>> GetAllAsync(CancellationToken cancellationToken = default) { return await _context.Set<TestingLocation>().Where(tl => tl.DeletedAt == null).OrderBy(tl => tl.Name).ToListAsync(cancellationToken); }

  public async Task<IEnumerable<TestingLocation>> GetByStatusAsync(LocationStatus status, CancellationToken cancellationToken = default) {
    return await _context.Set<TestingLocation>().Where(tl => tl.Status == status && tl.DeletedAt == null).OrderBy(tl => tl.Name).ToListAsync(cancellationToken);
  }

  public async Task<TestingLocation> CreateAsync(TestingLocation location, CancellationToken cancellationToken = default) {
    location.Id = Guid.NewGuid();
    location.Touch();

    _context.Set<TestingLocation>().Add(location);
    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return location;
  }

  public async Task<TestingLocation> UpdateAsync(TestingLocation location, CancellationToken cancellationToken = default) {
    var existingLocation = await _context.Set<TestingLocation>().FirstOrDefaultAsync(tl => tl.Id == location.Id && tl.DeletedAt == null, cancellationToken);

    if (existingLocation == null) { throw new InvalidOperationException($"Testing location with ID {location.Id} not found"); }

    // Update properties
    existingLocation.Name = location.Name;
    existingLocation.Description = location.Description;
    existingLocation.Address = location.Address;
    existingLocation.MaxTestersCapacity = location.MaxTestersCapacity;
    existingLocation.MaxProjectsCapacity = location.MaxProjectsCapacity;
    existingLocation.EquipmentAvailable = location.EquipmentAvailable;
    existingLocation.Status = location.Status;
    existingLocation.Touch();

    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return existingLocation;
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) {
    var location = await _context.Set<TestingLocation>().FirstOrDefaultAsync(tl => tl.Id == id && tl.DeletedAt == null, cancellationToken);

    if (location == null) { return false; }

    // Soft delete
    location.SoftDelete();
    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return true;
  }

  public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.Set<TestingLocation>().AnyAsync(tl => tl.Id == id && tl.DeletedAt == null, cancellationToken); }
}
