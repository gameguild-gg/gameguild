using GameGuild.Database;

namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Repository implementation for user profile data access operations
/// </summary>
public class UserProfileRepository(ApplicationDbContext context) : IUserProfileRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.FirstOrDefaultAsync(up => up.Id == id && up.DeletedAt == null, cancellationToken);
    }

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.FirstOrDefaultAsync(up => up.Id == userId && up.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default) { return await _context.UserProfiles.Where(up => up.DeletedAt == null).ToListAsync(cancellationToken); }

    public async Task<IReadOnlyList<UserProfile>> GetAllAsync(bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = includeDeleted ? _context.UserProfiles.IgnoreQueryFilters() : _context.UserProfiles.Where(up => up.DeletedAt == null);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<UserProfile> CreateAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync(cancellationToken);

        return userProfile;
    }

    public async Task<UserProfile> UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        _context.UserProfiles.Update(userProfile);
        await _context.SaveChangesAsync(cancellationToken);

        return userProfile;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserProfile? userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.Id == id, cancellationToken);

        if (userProfile == null) { return false; }

        _context.UserProfiles.Remove(userProfile);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserProfile? userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.Id == id && up.DeletedAt == null, cancellationToken);

        if (userProfile == null) { return false; }

        userProfile.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserProfile? userProfile = await _context.UserProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(up => up.Id == id && up.DeletedAt != null, cancellationToken);

        if (userProfile == null) { return false; }

        userProfile.Restore();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<UserProfile>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.IgnoreQueryFilters().Where(up => up.DeletedAt != null).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.AnyAsync(up => up.Id == userId && up.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfile>> SearchAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(searchTerm)
            ? await _context.UserProfiles.Where(up => up.DeletedAt == null).ToListAsync(cancellationToken)
            : await _context.UserProfiles.Where(up => up.DeletedAt == null && up.DisplayName!.Contains(searchTerm))
                .ToListAsync(cancellationToken);
    }

    public async Task<UserProfileStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? tenantId = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var fromDateTime = fromDate ?? DateTime.MinValue;
        var toDateTime = toDate ?? DateTime.MaxValue;

        var query = _context.UserProfiles.AsQueryable();

        // Apply tenant filter
        if (tenantId.HasValue)
            query = query.Where(up => EF.Property<Guid?>(up, "TenantId") == tenantId);

        var statistics = new UserProfileStatistics();

        // Total counts
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
            statistics.TotalUserProfiles = await query.CountAsync(cancellationToken);
            statistics.ActiveUserProfiles = await query.Where(up => up.DeletedAt == null).CountAsync(cancellationToken);
            statistics.DeletedUserProfiles = await query.Where(up => up.DeletedAt != null).CountAsync(cancellationToken);
        }
        else
        {
            statistics.TotalUserProfiles = await query.Where(up => up.DeletedAt == null).CountAsync(cancellationToken);
            statistics.ActiveUserProfiles = statistics.TotalUserProfiles;
            statistics.DeletedUserProfiles = 0;
        }

        // Date range statistics
        statistics.NewUserProfiles = await query.Where(up => up.CreatedAt >= fromDateTime && up.CreatedAt <= toDateTime).CountAsync(cancellationToken);
        statistics.UpdatedUserProfiles = await query.Where(up => up.UpdatedAt >= fromDateTime && up.UpdatedAt <= toDateTime && up.UpdatedAt != up.CreatedAt).CountAsync(cancellationToken);

        // Calculate average per day
        var daysDiff = (toDateTime - fromDateTime).TotalDays;
        if (daysDiff > 0)
            statistics.AverageNewUserProfilesPerDay = statistics.NewUserProfiles / daysDiff;

        // Display name patterns (common prefixes/titles)
        var displayNames = await query.Where(up => up.DisplayName != null).Select(up => up.DisplayName).ToListAsync(cancellationToken);
        var patterns = displayNames.Where(name => !string.IsNullOrEmpty(name))
            .SelectMany(name => name!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(word => word.Length > 2)
            .GroupBy(word => word.ToLower())
            .ToDictionary(g => g.Key, g => g.Count());
        statistics.DisplayNamePatterns = patterns.OrderByDescending(p => p.Value).Take(10).ToDictionary(p => p.Key, p => p.Value);

        // Tenant distribution (if multi-tenant)
        if (tenantId == null)
        {
            var tenantCounts = await query.GroupBy(up => EF.Property<Guid?>(up, "TenantId"))
                .Select(g => new { TenantId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            statistics.TenantDistribution = tenantCounts.ToDictionary(tc => tc.TenantId?.ToString() ?? "No Tenant", tc => tc.Count);
        }

        return statistics;
    }

    public async Task<bool> BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var userProfiles = await _context.UserProfiles.Where(up => ids.Contains(up.Id)).ToListAsync(cancellationToken);

        if (userProfiles.Count == 0) return false;

        _context.UserProfiles.RemoveRange(userProfiles);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> BulkRestoreAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var userProfiles = await _context.UserProfiles.IgnoreQueryFilters().Where(up => ids.Contains(up.Id) && up.DeletedAt != null).ToListAsync(cancellationToken);

        if (userProfiles.Count == 0) return false;

        foreach (UserProfile userProfile in userProfiles) { userProfile.Restore(); }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await _context.SaveChangesAsync(cancellationToken); }

    // Validation methods
    public async Task<bool> IsDisplayNameUniqueAsync(string displayName, Guid? excludeUserProfileId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.UserProfiles.Where(up => up.DisplayName == displayName && up.DeletedAt == null);

        if (excludeUserProfileId.HasValue) query = query.Where(up => up.Id != excludeUserProfileId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.UserProfiles.AnyAsync(up => up.Id == id && up.DeletedAt == null, cancellationToken); }

    public async Task<bool> DeletedExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.IgnoreQueryFilters().AnyAsync(up => up.Id == id && up.DeletedAt != null, cancellationToken);
    }
}
