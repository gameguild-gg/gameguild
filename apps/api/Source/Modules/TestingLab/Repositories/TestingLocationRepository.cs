using Microsoft.EntityFrameworkCore;
using GameGuild.Database;

namespace GameGuild.Modules.TestingLab;

public class TestingLocationRepository : ITestingLocationRepository
{
    private readonly ApplicationDbContext _context;

    public TestingLocationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TestingLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TestingLocations
            .Where(tl => tl.Id == id && tl.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<TestingLocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TestingLocations
            .Where(tl => tl.DeletedAt == null)
            .OrderBy(tl => tl.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TestingLocation>> GetByStatusAsync(LocationStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TestingLocations
            .Where(tl => tl.Status == status && tl.DeletedAt == null)
            .OrderBy(tl => tl.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TestingLocation> CreateAsync(TestingLocation location, CancellationToken cancellationToken = default)
    {
        location.Id = Guid.NewGuid();
        location.CreatedAt = DateTime.UtcNow;
        location.UpdatedAt = DateTime.UtcNow;

        _context.TestingLocations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        return location;
    }

    public async Task<TestingLocation> UpdateAsync(TestingLocation location, CancellationToken cancellationToken = default)
    {
        var existingLocation = await _context.TestingLocations
            .FirstOrDefaultAsync(tl => tl.Id == location.Id && tl.DeletedAt == null, cancellationToken);

        if (existingLocation == null)
        {
            throw new InvalidOperationException($"Testing location with ID {location.Id} not found");
        }

        // Update properties
        existingLocation.Name = location.Name;
        existingLocation.Description = location.Description;
        existingLocation.Address = location.Address;
        existingLocation.MaxTestersCapacity = location.MaxTestersCapacity;
        existingLocation.MaxProjectsCapacity = location.MaxProjectsCapacity;
        existingLocation.EquipmentAvailable = location.EquipmentAvailable;
        existingLocation.Status = location.Status;
        existingLocation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return existingLocation;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await _context.TestingLocations
            .FirstOrDefaultAsync(tl => tl.Id == id && tl.DeletedAt == null, cancellationToken);

        if (location == null)
        {
            return false;
        }

        // Soft delete
        location.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TestingLocations
            .AnyAsync(tl => tl.Id == id && tl.DeletedAt == null, cancellationToken);
    }
}
