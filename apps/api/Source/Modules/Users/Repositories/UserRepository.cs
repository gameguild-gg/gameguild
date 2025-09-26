using System.Linq.Expressions;
using GameGuild.Database;

namespace GameGuild.Modules.Users;

/// <summary>
/// Repository implementation for user data access operations
/// Adapter implementation following hexagonal architecture principles
/// </summary>
public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken); }

    public async Task<User?> GetByIdAsync(Guid id, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = includeDeleted ? _context.Users.IgnoreQueryFilters() : _context.Users.Where(u => u.DeletedAt == null);

        return await query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByIdWithCredentialsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.Include(u => u.Credentials).FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) { return await _context.Users.Where(u => u.DeletedAt == null).ToListAsync(cancellationToken); }

    public async Task<IEnumerable<User>> GetAllAsync(bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = includeDeleted ? _context.Users.IgnoreQueryFilters() : _context.Users.Where(u => u.DeletedAt == null);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetDeletedAsync(CancellationToken cancellationToken = default) { return await _context.Users.IgnoreQueryFilters().Where(u => u.DeletedAt != null).ToListAsync(cancellationToken); }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();

        return await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress != null && u.EmailAddress.Value == normalizedEmail && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.DeletedAt == null, cancellationToken);
    }

    public async Task<IEnumerable<User>> SearchAsync(string searchTerm, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = includeDeleted ? _context.Users.IgnoreQueryFilters() : _context.Users.Where(u => u.DeletedAt == null);

        return await query.Where(u => u.Name.Contains(searchTerm) || (u.EmailAddress != null && u.EmailAddress.Value.Contains(searchTerm)) || u.Username.Contains(searchTerm)).ToListAsync(cancellationToken);
    }

    public async Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _context.Users.CountAsync(u => u.DeletedAt == null, cancellationToken);
        var activeUsers = await _context.Users.CountAsync(u => u.DeletedAt == null && u.IsActive, cancellationToken);
        var inactiveUsers = totalUsers - activeUsers;
        var deletedUsers = await _context.Users.IgnoreQueryFilters().CountAsync(u => u.DeletedAt != null, cancellationToken);

        return new UserStatistics { TotalUsers = totalUsers, ActiveUsers = activeUsers, InactiveUsers = inactiveUsers, DeletedUsers = deletedUsers };
    }

    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.Username == username && u.DeletedAt == null);

        if (excludeUserId.HasValue) { query = query.Where(u => u.Id != excludeUserId.Value); }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var query = _context.Users.Where(u => u.EmailAddress != null && u.EmailAddress.Value == normalizedEmail && u.DeletedAt == null);

        if (excludeUserId.HasValue) { query = query.Where(u => u.Id != excludeUserId.Value); }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetUsernamesStartingWithAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return await _context.Users.Where(u => u.Username.StartsWith(prefix) && u.DeletedAt == null).Select(u => u.Username).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.DeletedAt == null);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Users.Where(predicate).Where(u => u.DeletedAt == null).ToListAsync(cancellationToken);
    }

    public async Task<User?> FirstOrDefaultAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Users.Where(predicate).Where(u => u.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Users.Where(predicate).Where(u => u.DeletedAt == null).AnyAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.DeletedAt == null);

        return predicate == null ? await query.CountAsync(cancellationToken) : await query.CountAsync(predicate, cancellationToken);
    }

    public async Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default)
    {
        _context.Users.AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task UpdateRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default)
    {
        _context.Users.UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default)
    {
        _context.Users.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken);

        if (user != null)
        {
            user.SoftDelete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt != null, cancellationToken);

        if (user != null)
        {
            user.Restore();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken);

        if (user == null) return false;

        user.IsActive = true;
        user.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken);

        if (user == null) return false;

        user.IsActive = false;
        user.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // Bulk operations
    public async Task<int> BulkActivateAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.Where(u => userIds.Contains(u.Id) && u.DeletedAt == null).ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            user.IsActive = true;
            user.Touch();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return users.Count;
    }

    public async Task<int> BulkDeactivateAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.Where(u => userIds.Contains(u.Id) && u.DeletedAt == null).ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            user.IsActive = false;
            user.Touch();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return users.Count;
    }

    public async Task<int> BulkSoftDeleteAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.Where(u => userIds.Contains(u.Id) && u.DeletedAt == null).ToListAsync(cancellationToken);

        foreach (var user in users) user.SoftDelete();

        await _context.SaveChangesAsync(cancellationToken);

        return users.Count;
    }

    public async Task<int> BulkRestoreAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.IgnoreQueryFilters().Where(u => userIds.Contains(u.Id) && u.DeletedAt != null).ToListAsync(cancellationToken);

        foreach (var user in users) user.Restore();

        await _context.SaveChangesAsync(cancellationToken);

        return users.Count;
    }

    public async Task<int> BulkHardDeleteAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.IgnoreQueryFilters().Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

        _context.Users.RemoveRange(users);
        await _context.SaveChangesAsync(cancellationToken);

        return users.Count;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { return await _context.SaveChangesAsync(cancellationToken); }
}
