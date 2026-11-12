using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

// Query contracts implemented by the focused query handler folders in this module.
// The per-field projection queries that previously lived here (custom fields, tags,
// avatar, banner, notification count/page/delivery settings, accessibility/privacy/
// notification preferences) were removed with issue #1277 batch 1: they had no
// handler, no route and no caller, and the live per-family GETs project the same
// fields from GetUserPreferencesQuery / GetUserNotificationsPagedQuery results
// (docs/identity/users/cqrs-wiring-matrix.md §1).
public sealed record GetUserMetadataQuery(Guid UserId) : IQuery<UserMetadataDto?>;

public sealed record GetUserPreferencesQuery(Guid UserId) : IQuery<UserPreferencesDto?>;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto?>;

public sealed record GetUserNotificationQuery(Guid UserId, Guid NotificationId) : IQuery<UserNotificationDetailDto?>;
