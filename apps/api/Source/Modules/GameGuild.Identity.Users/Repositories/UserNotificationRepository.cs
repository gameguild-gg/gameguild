using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     Repository interface for UserNotification
/// </summary>
public interface IUserNotificationRepository
{
    Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<UserNotification>> GetByIdsAsync(Guid userId, List<Guid> notificationIds, CancellationToken cancellationToken = default);

    Task<List<UserNotification>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    Task<(List<UserNotification> Notifications, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = "desc",
        bool? isArchived = null,
        string? type = null,
        bool? isRead = null,
        string? priority = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    );

    Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Dictionary<string, int>> GetUnreadCountByTypeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserNotification notification, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ArchiveAllAsync(Guid userId, int olderThanDays = 30, CancellationToken cancellationToken = default);

    Task DeleteArchivedAsync(Guid userId, int olderThanDays = 90, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     EntityFramework implementation of UserNotification repository
/// </summary>
public class UserNotificationRepository(IApplicationDbContext context) : IUserNotificationRepository
{
    public async Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserNotification>().FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<UserNotification>> GetByIdsAsync(Guid userId, List<Guid> notificationIds, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserNotification>()
            .Where(n => n.UserId == userId && notificationIds.Contains(n.Id) && n.DeletedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<UserNotification>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserNotification>()
            .Where(n => n.UserId == userId && n.DeletedAt == null && !n.IsArchived)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(List<UserNotification> Notifications, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = "desc",
        bool? isArchived = null,
        string? type = null,
        bool? isRead = null,
        string? priority = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.Set<UserNotification>().Where(n => n.UserId == userId && n.DeletedAt == null);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(n =>
                n.Title.ToLower().Contains(searchLower) ||
                n.Content.ToLower().Contains(searchLower));
        }

        // Apply filters
        if (isArchived.HasValue) { query = query.Where(n => n.IsArchived == isArchived.Value); }

        if (!string.IsNullOrEmpty(type)) { query = query.Where(n => n.Type == type); }

        if (isRead.HasValue) { query = query.Where(n => n.IsRead == isRead.Value); }

        if (!string.IsNullOrEmpty(priority) && Enum.TryParse(priority, true, out NotificationPriority priorityEnum)) { query = query.Where(n => n.Priority == priorityEnum); }

        if (fromDate.HasValue) { query = query.Where(n => n.CreatedAt >= fromDate.Value); }

        if (toDate.HasValue) { query = query.Where(n => n.CreatedAt <= toDate.Value); }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply sorting
        var isDescending = sortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) ?? true;
        query = sortBy?.ToLowerInvariant() switch
        {
            "priority" => isDescending ? query.OrderByDescending(n => n.Priority) : query.OrderBy(n => n.Priority),
            "type" => isDescending ? query.OrderByDescending(n => n.Type) : query.OrderBy(n => n.Type),
            "title" => isDescending ? query.OrderByDescending(n => n.Title) : query.OrderBy(n => n.Title),
            "createdat" => isDescending ? query.OrderByDescending(n => n.CreatedAt) : query.OrderBy(n => n.CreatedAt),
            _ => isDescending ? query.OrderByDescending(n => n.CreatedAt) : query.OrderBy(n => n.CreatedAt)
        };

        var notifications = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return (notifications, totalCount);
    }

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserNotification>().CountAsync(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null && !n.IsArchived, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Dictionary<string, int>> GetUnreadCountByTypeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserNotification>()
            .Where(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null && !n.IsArchived)
            .GroupBy(n => n.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default) { await context.Set<UserNotification>().AddAsync(notification, cancellationToken).ConfigureAwait(false); }

    public Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        context.Set<UserNotification>().Update(notification);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        context.Set<UserNotification>().Remove(notification); // Hard delete for notifications

        return Task.CompletedTask;
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await context.Set<UserNotification>()
            .Where(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, DateTime.UtcNow).SetProperty(n => n.UpdatedAt, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ArchiveAllAsync(Guid userId, int olderThanDays = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        await context.Set<UserNotification>()
            .Where(n => n.UserId == userId && !n.IsArchived && n.DeletedAt == null && n.CreatedAt < cutoffDate)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsArchived, true).SetProperty(n => n.ArchivedAt, DateTime.UtcNow).SetProperty(n => n.UpdatedAt, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteArchivedAsync(Guid userId, int olderThanDays = 90, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        await context.Set<UserNotification>().Where(n => n.UserId == userId && n.IsArchived && n.ArchivedAt < cutoffDate).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
