using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     Represents a unified user in the system combining identity, authentication, and profile.
///     Inherits from EntityBase to provide GUID IDs, version control, timestamps, and soft delete functionality.
/// </summary>
/// <remarks>
///     <para>
///         <b>Unified Entity:</b> This entity combines the former AuthUser (authentication) and User (profile)
///         entities into a single source of truth. PasswordHash is nullable to support OAuth-only users.
///     </para>
///     <para>
///         <b>Related Entities (Same Module):</b> User has 1:1 relationships with:
///         <list type="bullet">
///             <item><see cref="UserProfile"/> - Extended profile information (bio, avatar, social links)</item>
///             <item><see cref="UserMetadata"/> - Custom fields, tags, external references</item>
///             <item><see cref="UserPreferences"/> - Notification, privacy, localization settings</item>
///         </list>
///         And 1:many with <see cref="UserNotification"/> for notification history.
///     </para>
///     <para>
///         <b>Cross-Module Relationship:</b> Users can belong to multiple tenants via the
///         <c>TenantMember</c> entity in the <c>GameGuild.Tenants</c> module.
///         To keep modules decoupled, there is no navigation property to TenantMember. Instead, query
///         user memberships through <c>ITenantMemberRepository.GetByUserIdAsync(userId)</c>.
///     </para>
///     <para>
///         See also: <c>GameGuild.Identity.Tenants.Entities.TenantMember</c>
///     </para>
/// </remarks>
[Table("Users")]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
public class User : EntityBase, IUser
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public User() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial user data</param>
    public User(object partial) : base(partial) { }

    // ========================
    // IDENTITY FIELDS
    // ========================

    /// <summary>
    ///     Email address of the user (unique)
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     Optional username for display (unique if set)
    /// </summary>
    [MaxLength(256)]
    public string? Username { get; set; }

    /// <summary>
    ///     Full name of the user
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // ========================
    // AUTHENTICATION FIELDS
    // ========================

    /// <summary>
    ///     BCrypt password hash. Null for OAuth-only users.
    /// </summary>
    [MaxLength(512)]
    public string? PasswordHash { get; set; }

    /// <summary>
    ///     Whether the user's email has been verified
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    ///     Date and time of the user's last login
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // ========================
    // STATUS FIELDS
    // ========================

    /// <summary>
    ///     Whether this user is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Whether this user is suspended
    /// </summary>
    public bool IsSuspended { get; set; }

    /// <summary>
    ///     Gets the current user status as a value object for rich status operations.
    ///     Not mapped to database - computed from IsActive and IsSuspended.
    /// </summary>
    [NotMapped]
    public UserStatus Status => new(IsActive, IsSuspended);

    // ========================
    // PROFILE FIELDS
    // ========================

    /// <summary>
    ///     Optional phone number
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    ///     Date and time when the user was last seen/logged in
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    // ========================
    // NAVIGATION PROPERTIES
    // ========================

    /// <summary>
    ///     Extended profile information (bio, avatar, social links).
    ///     Lazy loaded, nullable if profile not yet created.
    /// </summary>
    public virtual UserProfile? Profile { get; set; }

    /// <summary>
    ///     Custom metadata (tags, external references, custom fields).
    ///     Lazy loaded, nullable if metadata not yet created.
    /// </summary>
    public virtual UserMetadata? Metadata { get; set; }

    /// <summary>
    ///     User preferences (notifications, privacy, localization).
    ///     Lazy loaded, nullable if preferences not yet created.
    /// </summary>
    public virtual UserPreferences? Preferences { get; set; }

    /// <summary>
    ///     Collection of notifications for this user.
    /// </summary>
    public virtual ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();

    // ========================
    // AUTHENTICATION METHODS
    // ========================

    /// <summary>
    ///     Set the password hash for the user
    /// </summary>
    /// <param name="passwordHash">BCrypt hash of the password</param>
    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        Touch();
    }

    /// <summary>
    ///     Check if the user has a password set (vs OAuth-only)
    /// </summary>
    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    /// <summary>
    ///     Record a successful login
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        LastSeenAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    ///     Mark email as verified
    /// </summary>
    public void VerifyEmail()
    {
        IsEmailVerified = true;
        Touch();
    }

    // ========================
    // STATUS METHODS
    // ========================

    /// <summary>
    ///     Activate the user
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    ///     Deactivate the user
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    ///     Suspend the user
    /// </summary>
    public void Suspend()
    {
        IsSuspended = true;
        Touch();
    }

    /// <summary>
    ///     Unsuspend the user
    /// </summary>
    public void Unsuspend()
    {
        IsSuspended = false;
        Touch();
    }

    // ========================
    // PROFILE METHODS
    // ========================

    /// <summary>
    ///     Update user information
    /// </summary>
    /// <param name="name">New name</param>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdateInfo(string name, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        PhoneNumber = phoneNumber;
        Touch();
    }

    /// <summary>
    ///     Record user activity (last seen)
    /// </summary>
    public void RecordActivity()
    {
        LastSeenAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    ///     Update the user's name
    /// </summary>
    /// <param name="name">New name</param>
    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Touch();
    }

    /// <summary>
    ///     Update the user's phone number
    /// </summary>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdatePhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
        Touch();
    }

    // ========================
    // FACTORY METHODS
    // ========================

    /// <summary>
    ///     Static factory method to create a new user with password authentication
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's full name</param>
    /// <param name="passwordHash">BCrypt password hash</param>
    /// <param name="username">Optional username</param>
    /// <returns>New User instance</returns>
    public static User CreateWithPassword(string email, string name, string passwordHash, string? username = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Email = email.ToLowerInvariant(),
            Name = name,
            Username = username,
            PasswordHash = passwordHash,
            IsActive = true
        };
    }

    /// <summary>
    ///     Static factory method to create a new OAuth-only user
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's full name</param>
    /// <returns>New User instance without password</returns>
    public static User CreateOAuthUser(string email, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new User
        {
            Email = email.ToLowerInvariant(),
            Name = name,
            PasswordHash = null, // OAuth-only user
            IsActive = true,
            IsEmailVerified = true // OAuth emails are pre-verified
        };
    }

    /// <summary>
    ///     Static factory method to create a new user (legacy compatibility)
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's full name</param>
    /// <param name="phoneNumber">Optional phone number</param>
    /// <returns>New User instance</returns>
    public static User Create(string email, string name, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new User { Email = email.ToLowerInvariant(), Name = name, PhoneNumber = phoneNumber, IsActive = true };
    }
}
