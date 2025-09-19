using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Database;
using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Source.Core.Users;

/// <summary>
/// Enumeration of privacy visibility levels
/// </summary>
public enum PrivacyLevel {
    /// <summary>Public - visible to everyone</summary>
    Public = 0,

    /// <summary>Tenant Members - visible to users within the same tenant</summary>
    TenantMembers = 1,

    /// <summary>Friends - visible only to friends/connections</summary>
    Friends = 2,

    /// <summary>Private - visible only to the user themselves</summary>
    Private = 3
}

/// <summary>
/// Represents privacy settings for a user
/// Allows granular control over what information is visible to different audiences
/// </summary>
[Table("UserPrivacySettings")]
[Index(nameof(UserId), IsUnique = true)]
public class UserPrivacySettings : EntityBase, ITenantable {
    /// <summary>
    /// Reference to the user these settings belong to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant context for privacy settings (allows tenant-specific privacy policies)
    /// </summary>
    public override Tenant? Tenant { get; set; }

    // Profile Information Privacy
    /// <summary>Privacy level for user's real name</summary>
    public PrivacyLevel NameVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for user's email address</summary>
    public PrivacyLevel EmailVisibility { get; set; } = PrivacyLevel.Private;

    /// <summary>Privacy level for user's phone number</summary>
    public PrivacyLevel PhoneVisibility { get; set; } = PrivacyLevel.Private;

    /// <summary>Privacy level for user's profile picture/avatar</summary>
    public PrivacyLevel AvatarVisibility { get; set; } = PrivacyLevel.Public;

    /// <summary>Privacy level for user's bio/description</summary>
    public PrivacyLevel BioVisibility { get; set; } = PrivacyLevel.TenantMembers;

    // Activity Privacy
    /// <summary>Privacy level for user's last seen/activity status</summary>
    public PrivacyLevel LastSeenVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for user's online status</summary>
    public PrivacyLevel OnlineStatusVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for user's activity feed</summary>
    public PrivacyLevel ActivityFeedVisibility { get; set; } = PrivacyLevel.TenantMembers;

    // Content Privacy
    /// <summary>Privacy level for user's posts/content</summary>
    public PrivacyLevel PostsVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for user's comments</summary>
    public PrivacyLevel CommentsVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for user's achievements</summary>
    public PrivacyLevel AchievementsVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for user's projects</summary>
    public PrivacyLevel ProjectsVisibility { get; set; } = PrivacyLevel.TenantMembers;

    // Social Privacy
    /// <summary>Privacy level for user's friends/connections list</summary>
    public PrivacyLevel FriendsListVisibility { get; set; } = PrivacyLevel.Friends;

    /// <summary>Privacy level for user's followers</summary>
    public PrivacyLevel FollowersVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for who the user is following</summary>
    public PrivacyLevel FollowingVisibility { get; set; } = PrivacyLevel.TenantMembers;

    // Statistics Privacy
    /// <summary>Privacy level for user's statistics (scores, rankings, etc.)</summary>
    public PrivacyLevel StatisticsVisibility { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Privacy level for user's gaming history</summary>
    public PrivacyLevel GamingHistoryVisibility { get; set; } = PrivacyLevel.TenantMembers;

    // Communication Privacy
    /// <summary>Who can send direct messages to this user</summary>
    public PrivacyLevel DirectMessagesAllowed { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Who can mention this user in posts/comments</summary>
    public PrivacyLevel MentionsAllowed { get; set; } = PrivacyLevel.TenantMembers;

    /// <summary>Who can invite this user to events/groups</summary>
    public PrivacyLevel InvitationsAllowed { get; set; } = PrivacyLevel.TenantMembers;

    // Notification Privacy
    /// <summary>Whether to show this user in search results</summary>
    public bool ShowInSearch { get; set; } = true;

    /// <summary>Whether to show this user in member directories</summary>
    public bool ShowInDirectory { get; set; } = true;

    /// <summary>Whether to allow others to see when this user reads messages</summary>
    public bool ShowReadReceipts { get; set; } = true;

    /// <summary>Whether to show this user's typing indicators</summary>
    public bool ShowTypingIndicators { get; set; } = true;

    // Data Privacy
    /// <summary>Whether to allow analytics tracking</summary>
    public bool AllowAnalytics { get; set; } = true;

    /// <summary>Whether to allow personalized recommendations</summary>
    public bool AllowPersonalization { get; set; } = true;

    /// <summary>Whether to allow third-party integrations</summary>
    public bool AllowThirdPartyIntegrations { get; set; } = false;

    /// <summary>
    /// Custom privacy settings as JSON for future extensibility
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? CustomSettings { get; set; }

    /// <summary>
    /// Indicates whether this resource is accessible across all tenants
    /// </summary>
    public bool IsGlobal => Tenant == null;

    /// <summary>
    /// Factory method to create default privacy settings for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenant">Tenant context</param>
    /// <returns>Default privacy settings</returns>
    public static UserPrivacySettings CreateDefault(Guid userId, Tenant? tenant = null) {
        return new UserPrivacySettings {
            UserId = userId,
            Tenant = tenant,
            // All other properties will use their default values from the property declarations
        };
    }

    /// <summary>
    /// Create privacy settings optimized for public users
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenant">Tenant context</param>
    /// <returns>Public-friendly privacy settings</returns>
    public static UserPrivacySettings CreatePublicProfile(Guid userId, Tenant? tenant = null) {
        return new UserPrivacySettings {
            UserId = userId,
            Tenant = tenant,
            NameVisibility = PrivacyLevel.Public,
            AvatarVisibility = PrivacyLevel.Public,
            BioVisibility = PrivacyLevel.Public,
            PostsVisibility = PrivacyLevel.Public,
            AchievementsVisibility = PrivacyLevel.Public,
            ProjectsVisibility = PrivacyLevel.Public,
            StatisticsVisibility = PrivacyLevel.Public,
            ShowInSearch = true,
            ShowInDirectory = true
        };
    }

    /// <summary>
    /// Create privacy settings optimized for private users
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenant">Tenant context</param>
    /// <returns>Privacy-focused settings</returns>
    public static UserPrivacySettings CreatePrivateProfile(Guid userId, Tenant? tenant = null) {
        return new UserPrivacySettings {
            UserId = userId,
            Tenant = tenant,
            NameVisibility = PrivacyLevel.Friends,
            EmailVisibility = PrivacyLevel.Private,
            PhoneVisibility = PrivacyLevel.Private,
            AvatarVisibility = PrivacyLevel.Friends,
            BioVisibility = PrivacyLevel.Friends,
            LastSeenVisibility = PrivacyLevel.Private,
            OnlineStatusVisibility = PrivacyLevel.Private,
            ActivityFeedVisibility = PrivacyLevel.Friends,
            PostsVisibility = PrivacyLevel.Friends,
            CommentsVisibility = PrivacyLevel.Friends,
            AchievementsVisibility = PrivacyLevel.Private,
            ProjectsVisibility = PrivacyLevel.Private,
            FriendsListVisibility = PrivacyLevel.Private,
            FollowersVisibility = PrivacyLevel.Private,
            FollowingVisibility = PrivacyLevel.Private,
            StatisticsVisibility = PrivacyLevel.Private,
            GamingHistoryVisibility = PrivacyLevel.Private,
            DirectMessagesAllowed = PrivacyLevel.Friends,
            MentionsAllowed = PrivacyLevel.Friends,
            InvitationsAllowed = PrivacyLevel.Friends,
            ShowInSearch = false,
            ShowInDirectory = false,
            ShowReadReceipts = false,
            ShowTypingIndicators = false,
            AllowAnalytics = false,
            AllowPersonalization = false,
            AllowThirdPartyIntegrations = false
        };
    }
}

/// <summary>
/// Represents a privacy audit log entry for tracking changes to privacy settings
/// </summary>
[Table("UserPrivacyAuditLog")]
[Index(nameof(UserId), nameof(CreatedAt))]
public class UserPrivacyAuditLog : EntityBase, ITenantable {
    /// <summary>
    /// Reference to the user whose privacy settings changed
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant context for this audit entry
    /// </summary>
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Type of privacy change that occurred
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the privacy setting that was changed
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SettingName { get; set; } = string.Empty;

    /// <summary>
    /// Previous value of the setting
    /// </summary>
    [MaxLength(100)]
    public string? OldValue { get; set; }

    /// <summary>
    /// New value of the setting
    /// </summary>
    [MaxLength(100)]
    public string? NewValue { get; set; }

    /// <summary>
    /// User who made the change (if different from UserId, e.g., admin)
    /// </summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>
    /// IP address from which the change was made
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client that made the change
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context or reason for the change
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Indicates whether this resource is accessible across all tenants
    /// </summary>
    public bool IsGlobal => Tenant == null;
}