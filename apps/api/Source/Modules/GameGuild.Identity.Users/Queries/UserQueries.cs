using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

// Query contracts implemented by the focused query handler folders in this module.
public sealed record GetUserMetadataQuery(Guid UserId) : IQuery<UserMetadataDto?>;

public sealed record GetUserCustomFieldsQuery(Guid UserId) : IQuery<Dictionary<string, object?>>;

public sealed record GetUserTagsQuery(Guid UserId) : IQuery<List<string>>;

public sealed record GetUserPreferencesQuery(Guid UserId) : IQuery<UserPreferencesDto?>;

public sealed record GetUserNotificationPreferencesQuery(Guid UserId) : IQuery<UserNotificationPreferencesDto>;

public sealed record GetUserAccessibilityPreferencesQuery(Guid UserId) : IQuery<UserAccessibilityPreferencesDto>;

public sealed record GetUserPrivacyPreferencesQuery(Guid UserId) : IQuery<UserPrivacyPreferencesDto>;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto?>;

public sealed record GetUserAvatarQuery(Guid UserId) : IQuery<UserAvatarDto?>;

public sealed record GetUserBannerQuery(Guid UserId) : IQuery<UserBannerDto?>;

public sealed record GetUserNotificationsPageQuery(Guid UserId, int Page, int PageSize, string? Type, bool? IsRead, string? Priority, DateTimeOffset? FromDate, DateTimeOffset? ToDate) : IQuery<List<UserNotificationDto>>;

public sealed record GetUserNotificationCountQuery(Guid UserId) : IQuery<UserNotificationCountDto>;

public sealed record GetUserNotificationQuery(Guid UserId, Guid NotificationId) : IQuery<UserNotificationDetailDto?>;

public sealed record GetUserNotificationDeliverySettingsQuery(Guid UserId) : IQuery<UserNotificationDeliverySettingsDto>;
