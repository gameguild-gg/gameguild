using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     Repository interface for UserProfile
/// </summary>
public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<UserProfile> Profiles, int TotalCount)> GetProfilesPagedAsync(
        string? search,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserProfile profile, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     EntityFramework implementation of UserProfile repository
/// </summary>
public class UserProfileRepository(IApplicationDbContext context) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var query = context.Set<UserProfile>().Where(up => up.UserId == userId && up.DeletedAt == null);
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = context.Set<UserProfile>().Where(up => up.Id == id && up.DeletedAt == null);
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(List<UserProfile> Profiles, int TotalCount)> GetProfilesPagedAsync(
        string? search,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<UserProfile>().Where(up => up.DeletedAt == null);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(up =>
                (up.DisplayName != null && up.DisplayName.ToLower().Contains(searchLower)) ||
                (up.Bio != null && up.Bio.ToLower().Contains(searchLower)) ||
                (up.Location != null && up.Location.ToLower().Contains(searchLower)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply sorting
        var isDescending = sortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) ?? false;
        query = sortBy?.ToLowerInvariant() switch
        {
            "displayname" => isDescending ? query.OrderByDescending(up => up.DisplayName) : query.OrderBy(up => up.DisplayName),
            "location" => isDescending ? query.OrderByDescending(up => up.Location) : query.OrderBy(up => up.Location),
            "updatedat" => isDescending ? query.OrderByDescending(up => up.UpdatedAt) : query.OrderBy(up => up.UpdatedAt),
            "createdat" => isDescending ? query.OrderByDescending(up => up.CreatedAt) : query.OrderBy(up => up.CreatedAt),
            _ => isDescending ? query.OrderByDescending(up => up.CreatedAt) : query.OrderBy(up => up.CreatedAt)
        };

        // Apply pagination
        var profiles = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (profiles, totalCount);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default) { await context.Set<UserProfile>().AddAsync(profile, cancellationToken).ConfigureAwait(false); }

    public Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        context.Set<UserProfile>().Update(profile);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.SoftDelete();
        context.Set<UserProfile>().Update(profile);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
