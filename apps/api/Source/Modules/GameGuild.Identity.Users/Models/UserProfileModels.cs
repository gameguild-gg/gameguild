namespace GameGuild.Identity.Users;

/// <summary>
///     Data transfer object for user profile
/// </summary>
/// <param name="Id">The unique identifier for the user profile</param>
/// <param name="UserId">The user identifier that this profile belongs to</param>
/// <param name="DisplayName">User's display name</param>
/// <param name="Bio">User's biography</param>
/// <param name="Location">User's location</param>
/// <param name="Website">User's website URL</param>
/// <param name="AvatarUrl">URL to user's avatar image</param>
/// <param name="BannerUrl">URL to user's banner image</param>
/// <param name="TimeZone">User's preferred timezone</param>
/// <param name="Language">User's preferred language</param>
/// <param name="ProfileVisibility">Profile visibility setting</param>
/// <param name="ShowEmail">Whether to show email in profile</param>
/// <param name="ShowLocation">Whether to show location in profile</param>
/// <param name="CreatedAt">When the profile was created</param>
/// <param name="UpdatedAt">When the profile was last updated</param>
/// <param name="Version">Version for optimistic concurrency control</param>
public record UserProfileDto(
    Guid Id,
    Guid UserId,
    string? DisplayName,
    string? Bio,
    string? Location,
    string? Website,
    string? AvatarUrl,
    string? BannerUrl,
    string? TimeZone,
    string? Language,
    string ProfileVisibility,
    bool ShowEmail,
    bool ShowLocation,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    byte[ ] Version
);

/// <summary>
///     Request model for updating user profile
/// </summary>
/// <param name="DisplayName">Display name to update</param>
/// <param name="Bio">Biography to update</param>
/// <param name="Location">Location to update</param>
/// <param name="Website">Website to update</param>
/// <param name="TimeZone">Timezone to update</param>
/// <param name="Language">Language to update</param>
/// <param name="ProfileVisibility">Profile visibility to update</param>
/// <param name="ShowEmail">Whether to show email</param>
/// <param name="ShowLocation">Whether to show location</param>
public record UpdateUserProfileRequest(
    string? DisplayName = null,
    string? Bio = null,
    string? Location = null,
    string? Website = null,
    string? TimeZone = null,
    string? Language = null,
    string? ProfileVisibility = null,
    bool? ShowEmail = null,
    bool? ShowLocation = null
);

/// <summary>
///     Request model for completely replacing user profile
/// </summary>
/// <param name="DisplayName">Display name</param>
/// <param name="Bio">Biography</param>
/// <param name="Location">Location</param>
/// <param name="Website">Website URL</param>
/// <param name="TimeZone">Preferred timezone</param>
/// <param name="Language">Preferred language</param>
/// <param name="ProfileVisibility">Profile visibility setting</param>
/// <param name="ShowEmail">Whether to show email in profile</param>
/// <param name="ShowLocation">Whether to show location in profile</param>
public record ReplaceUserProfileRequest(string? DisplayName, string? Bio, string? Location, string? Website, string? TimeZone, string? Language, string ProfileVisibility, bool ShowEmail, bool ShowLocation);

/// <summary>
///     Data transfer object for user avatar
/// </summary>
/// <param name="AvatarUrl">URL to the avatar image</param>
/// <param name="UploadedAt">When the avatar was uploaded</param>
/// <param name="FileSize">Size of the avatar file in bytes</param>
/// <param name="ContentType">MIME type of the avatar file</param>
public record UserAvatarDto(string AvatarUrl, DateTimeOffset UploadedAt, long FileSize, string ContentType);

/// <summary>
///     Request model for uploading user avatar
/// </summary>
/// <param name="ImageData">Base64 encoded image data</param>
/// <param name="ContentType">MIME type of the image</param>
/// <param name="FileName">Original file name</param>
public record UploadUserAvatarRequest(string ImageData, string ContentType, string FileName);

/// <summary>
///     Data transfer object for user banner
/// </summary>
/// <param name="BannerUrl">URL to the banner image</param>
/// <param name="UploadedAt">When the banner was uploaded</param>
/// <param name="FileSize">Size of the banner file in bytes</param>
/// <param name="ContentType">MIME type of the banner file</param>
public record UserBannerDto(string BannerUrl, DateTimeOffset UploadedAt, long FileSize, string ContentType);

/// <summary>
///     Request model for uploading user banner
/// </summary>
/// <param name="ImageData">Base64 encoded image data</param>
/// <param name="ContentType">MIME type of the image</param>
/// <param name="FileName">Original file name</param>
public record UploadUserBannerRequest(string ImageData, string ContentType, string FileName);



