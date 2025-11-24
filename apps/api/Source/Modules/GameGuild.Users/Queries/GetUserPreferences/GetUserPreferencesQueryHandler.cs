using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;

namespace GameGuild.Users.Queries.GetUserPreferences;

/// <summary>
/// Query handler for getting user preferences
/// </summary>
public class GetUserPreferencesQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserPreferencesQuery, UserPreferencesDto?>
{
    public async Task<UserPreferencesDto?> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        
        if (user == null)
            return null;

        // Return default preferences since User entity doesn't have preferences yet
        return new UserPreferencesDto(
            Id: Guid.NewGuid(),
            UserId: user.Id,
            GeneralPreferences: new Dictionary<string, object?>
            {
                ["theme"] = "system",
                ["language"] = "en",
                ["timezone"] = "UTC"
            },
            NotificationPreferences: new Dictionary<string, object?>
            {
                ["emailEnabled"] = true,
                ["pushEnabled"] = true,
                ["smsEnabled"] = false
            },
            AccessibilityPreferences: new Dictionary<string, object?>
            {
                ["highContrast"] = false,
                ["largeText"] = false,
                ["reducedMotion"] = false
            },
            PrivacyPreferences: new Dictionary<string, object?>
            {
                ["profileVisibility"] = "public",
                ["activityTracking"] = true
            },
            LocalizationPreferences: new Dictionary<string, object?>
            {
                ["Language"] = "en-US",
                ["Timezone"] = "UTC",
                ["DateFormat"] = "MM/dd/yyyy",
                ["TimeFormat"] = "12h",
                ["Currency"] = "USD"
            },
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt,
            Version: new byte[] { 1 }
        );
    }
}
