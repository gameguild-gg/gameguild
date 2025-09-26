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
        IQueryable<UserProfile> query = includeDeleted ? _context.UserProfiles.IgnoreQueryFilters() : _context.UserProfiles.Where(up => up.DeletedAt == null);

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
            : await _context.UserProfiles.Where(up => up.DeletedAt == null && (up.GivenName.Contains(searchTerm!) || up.FamilyName.Contains(searchTerm!) || up.DisplayName.Contains(searchTerm!)))
                .ToListAsync(cancellationToken);
    }

    public async Task<UserProfileStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        int totalCount = await _context.UserProfiles.CountAsync(up => up.DeletedAt == null, cancellationToken);

        int deletedCount = await _context.UserProfiles.IgnoreQueryFilters().CountAsync(up => up.DeletedAt != null, cancellationToken);

        return new UserProfileStatistics { TotalUserProfiles = totalCount + deletedCount, ActiveUserProfiles = totalCount, DeletedUserProfiles = deletedCount };
    }

    public async Task<bool> BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        List<UserProfile> userProfiles = await _context.UserProfiles.Where(up => ids.Contains(up.Id)).ToListAsync(cancellationToken);

        if (userProfiles.Count == 0) { return false; }

        _context.UserProfiles.RemoveRange(userProfiles);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> BulkRestoreAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        List<UserProfile> userProfiles = await _context.UserProfiles.IgnoreQueryFilters().Where(up => ids.Contains(up.Id) && up.DeletedAt != null).ToListAsync(cancellationToken);

        if (userProfiles.Count == 0) { return false; }

        foreach (UserProfile userProfile in userProfiles) { userProfile.Restore(); }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await _context.SaveChangesAsync(cancellationToken); }

    // Validation methods
    public async Task<bool> IsDisplayNameUniqueAsync(string displayName, Guid? excludeUserProfileId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<UserProfile> query = _context.UserProfiles.Where(up => up.DisplayName == displayName && up.DeletedAt == null);

        if (excludeUserProfileId.HasValue)
        {
            query = query.Where(up => up.Id != excludeUserProfileId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.AnyAsync(up => up.Id == id && up.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> DeletedExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.IgnoreQueryFilters().AnyAsync(up => up.Id == id && up.DeletedAt != null, cancellationToken);
    }
}
