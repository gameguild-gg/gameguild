using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
/// Query handler for getting user preferences
/// </summary>
public sealed class GetUserPreferencesQueryHandler(
    IUserRepository userRepository,
    IUserPreferencesRepository preferencesRepository) : IQueryHandler<GetUserPreferencesQuery, UserPreferencesDto?>
{
    public async Task<UserPreferencesDto?> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (user == null)
            return null;

        var preferences = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        return new UserPreferencesDto(
            Id: preferences?.Id ?? Guid.NewGuid(),
            UserId: user.Id,
            GeneralPreferences: BuildPreferences(
                new Dictionary<string, object?>
                {
                    ["theme"] = "system",
                    ["language"] = "en",
                    ["timezone"] = "UTC"
                },
                preferences?.GetGeneralPreferences()),
            NotificationPreferences: BuildPreferences(
                new Dictionary<string, object?>
                {
                    ["emailEnabled"] = true,
                    ["pushEnabled"] = true,
                    ["smsEnabled"] = false,
                    ["inAppEnabled"] = true,
                    ["frequency"] = "immediate",
                    ["quietHours"] = new Dictionary<string, object?>(StringComparer.Ordinal),
                    ["categoryPreferences"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                },
                preferences?.GetNotificationPreferences()),
            AccessibilityPreferences: BuildPreferences(
                new Dictionary<string, object?>
                {
                    ["highContrast"] = false,
                    ["largeText"] = false,
                    ["screenReader"] = false,
                    ["reducedMotion"] = false,
                    ["keyboardNavigation"] = false,
                    ["fontSize"] = 16,
                    ["colorScheme"] = "system",
                    ["customSettings"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                },
                preferences?.GetAccessibilityPreferences()),
            PrivacyPreferences: BuildPreferences(
                new Dictionary<string, object?>
                {
                    ["profileVisibility"] = "public",
                    ["activityTracking"] = true,
                    ["dataCollection"] = new Dictionary<string, object?>(StringComparer.Ordinal),
                    ["thirdPartySharing"] = new Dictionary<string, object?>(StringComparer.Ordinal),
                    ["marketingEmails"] = false,
                    ["analyticsCookies"] = true,
                    ["personalizedContent"] = true,
                    ["customSettings"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                },
                preferences?.GetPrivacyPreferences()),
            LocalizationPreferences: BuildPreferences(
                new Dictionary<string, object?>
                {
                    ["Language"] = "en-US",
                    ["Timezone"] = "UTC",
                    ["DateFormat"] = "MM/dd/yyyy",
                    ["TimeFormat"] = "12h",
                    ["Currency"] = "USD",
                    ["NumberFormat"] = new Dictionary<string, object?>(StringComparer.Ordinal),
                    ["CustomSettings"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                },
                preferences?.GetLocalizationPreferences()),
            CreatedAt: preferences?.CreatedAt ?? user.CreatedAt,
            UpdatedAt: preferences?.UpdatedAt ?? user.UpdatedAt,
            Version: BitConverter.GetBytes(preferences?.Version ?? 1)
        );
    }

    private static Dictionary<string, System.Text.Json.JsonElement> BuildPreferences(
        Dictionary<string, object?> defaults,
        Dictionary<string, object?>? stored)
    {
        if (stored != null)
        {
            foreach (var preference in stored)
            {
                defaults[preference.Key] = preference.Value;
            }
        }

        return JsonValueDictionary.ToJsonElements(defaults);
    }
}
