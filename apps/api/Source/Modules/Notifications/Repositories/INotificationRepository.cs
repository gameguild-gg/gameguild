using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameGuild.Modules.Notifications;

/// <summary>
/// Repository for notification entities.
/// </summary>
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetByUserIdAsync(Guid userId, NotificationFilter? filter = null, CancellationToken cancellationToken = default);
    Task CreateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for notification preference entities.
/// </summary>
public interface INotificationPreferenceRepository
{
    Task<List<NotificationPreference>> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task CreateAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
