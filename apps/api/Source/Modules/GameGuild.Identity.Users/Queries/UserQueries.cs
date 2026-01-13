using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

// Placeholder queries - these need proper implementations
public record GetUserMetadataQuery(Guid UserId) : IQuery<UserMetadataDto?>;

public record GetUserCustomFieldsQuery(Guid UserId) : IQuery<Dictionary<string, object?>>;

public record GetUserTagsQuery(Guid UserId) : IQuery<List<string>>;

public record GetUserPreferencesQuery(Guid UserId) : IQuery<UserPreferencesDto?>;

public record GetUserNotificationPreferencesQuery(Guid UserId) : IQuery<UserNotificationPreferencesDto>;

public record GetUserAccessibilityPreferencesQuery(Guid UserId) : IQuery<UserAccessibilityPreferencesDto>;

public record GetUserPrivacyPreferencesQuery(Guid UserId) : IQuery<UserPrivacyPreferencesDto>;

// Note: Profile queries have been moved to GameGuild.Social.Profiles module
// Use GetUserProfileQuery, GetUserAvatarQuery, GetUserBannerQuery from there

public record GetUserNotificationsPageQuery(Guid UserId, int Page, int PageSize, string? Type, bool? IsRead, string? Priority, DateTimeOffset? FromDate, DateTimeOffset? ToDate) : IQuery<List<UserNotificationDto>>;

public record GetUserNotificationCountQuery(Guid UserId) : IQuery<UserNotificationCountDto>;

public record GetUserNotificationQuery(Guid UserId, Guid NotificationId) : IQuery<UserNotificationDetailDto?>;

public record GetUserNotificationDeliverySettingsQuery(Guid UserId) : IQuery<UserNotificationDeliverySettingsDto>;
