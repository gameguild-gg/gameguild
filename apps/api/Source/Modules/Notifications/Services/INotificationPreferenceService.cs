using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameGuild.Modules.Notifications;

/// <summary>
/// Service for managing notification preferences.
/// </summary>
public interface INotificationPreferenceService
{
    Task<List<NotificationPreference>> GetPreferencesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task UpdatePreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
    Task ResetToDefaultsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of notification preference service.
/// </summary>
public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly INotificationPreferenceRepository _repository;

    public NotificationPreferenceService(INotificationPreferenceRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<List<NotificationPreference>> GetPreferencesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var preferences = await _repository.GetByUserIdAsync(userId, tenantId, cancellationToken);

        // Create defaults if none exist
        if (preferences.Count == 0)
        {
            preferences = CreateDefaultPreferences(userId, tenantId);
            foreach (var pref in preferences)
            {
                await _repository.CreateAsync(pref, cancellationToken);
            }
        }

        return preferences;
    }

    public async Task UpdatePreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        preference.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(preference, cancellationToken);
    }

    public async Task ResetToDefaultsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteByUserIdAsync(userId, tenantId, cancellationToken);

        var defaults = CreateDefaultPreferences(userId, tenantId);
        foreach (var pref in defaults)
        {
            await _repository.CreateAsync(pref, cancellationToken);
        }
    }

    private List<NotificationPreference> CreateDefaultPreferences(Guid userId, Guid tenantId)
    {
        var preferences = new List<NotificationPreference>();

        foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
        {
            preferences.Add(new NotificationPreference
            {
                UserId = userId,
                TenantId = tenantId,
                NotificationType = type,
                EnabledChannels = NotificationChannel.All,
                IsEnabled = true
            });
        }

        return preferences;
    }
}
