namespace GameGuild.Modules.Users;

/// <summary>
/// Represents a user consent record for GDPR compliance
/// </summary>
[Table("consent_records")]
[Index(nameof(UserId), nameof(ConsentType), nameof(FeatureId))]
public sealed class ConsentRecord : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>
    /// Type of consent (e.g., "marketing", "analytics", "data-sharing", "cookies")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ConsentType { get; set; } = string.Empty;

    /// <summary>
    /// Specific feature or purpose (e.g., "email-newsletters", "product-recommendations")
    /// </summary>
    [MaxLength(100)]
    public string? FeatureId { get; set; }

    /// <summary>
    /// Whether consent was given or denied
    /// </summary>
    [Required]
    public bool IsGranted { get; set; }

    /// <summary>
    /// Version of the consent terms accepted
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ConsentVersion { get; set; } = "1.0";

    /// <summary>
    /// When consent was given or revoked
    /// </summary>
    [Required]
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When consent expires (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// IP address from which consent was given
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Source of consent (e.g., "web", "mobile-app", "api", "admin")
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = "web";

    /// <summary>
    /// Additional consent metadata (JSON)
    /// </summary>
    [MaxLength(1000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// When consent was last modified
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// Check if consent is expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    /// Check if consent is active (granted and not expired)
    /// </summary>
    public bool IsActive => IsGranted && !IsExpired;

    /// <summary>
    /// Revoke consent
    /// </summary>
    public void Revoke()
    {
        IsGranted = false;
        LastModifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Grant consent
    /// </summary>
    public void Grant(string? ipAddress = null, string? userAgent = null)
    {
        IsGranted = true;
        ConsentedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        if (ipAddress != null) IpAddress = ipAddress;
        if (userAgent != null) UserAgent = userAgent;
    }
}

/// <summary>
/// Represents privacy preferences for a user
/// </summary>
[Table("privacy_preferences")]
[Index(nameof(UserId), IsUnique = true)]
public sealed class PrivacyPreference : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>
    /// Data visibility level
    /// </summary>
    public DataVisibilityLevel VisibilityLevel { get; set; } = DataVisibilityLevel.FriendsOnly;

    /// <summary>
    /// Allow profile to be indexed by search engines
    /// </summary>
    public bool AllowSearchEngineIndexing { get; set; } = false;

    /// <summary>
    /// Show profile in public directories
    /// </summary>
    public bool ShowInPublicDirectory { get; set; } = true;

    /// <summary>
    /// Allow data to be used for analytics
    /// </summary>
    public bool AllowAnalytics { get; set; } = true;

    /// <summary>
    /// Allow personalization/recommendations
    /// </summary>
    public bool AllowPersonalization { get; set; } = true;

    /// <summary>
    /// Allow third-party data sharing
    /// </summary>
    public bool AllowThirdPartySharing { get; set; } = false;

    /// <summary>
    /// Allow activity tracking (logging user actions)
    /// </summary>
    public bool AllowActivityTracking { get; set; } = true;

    /// <summary>
    /// Allow location tracking
    /// </summary>
    public bool AllowLocationTracking { get; set; } = false;

    /// <summary>
    /// Data retention period preference (in days, null = default)
    /// </summary>
    public int? DataRetentionDays { get; set; }

    /// <summary>
    /// Additional privacy settings (JSON)
    /// </summary>
    [MaxLength(2000)]
    public string? AdditionalSettings { get; set; }

    /// <summary>
    /// When preferences were last reviewed by user
    /// </summary>
    public DateTime? LastReviewedAt { get; set; }

    /// <summary>
    /// Update last reviewed timestamp
    /// </summary>
    public void MarkAsReviewed()
    {
        LastReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Data visibility levels for user profiles
/// </summary>
public enum DataVisibilityLevel
{
    Private = 0,        // Only user can see
    FriendsOnly = 1,    // Only connections/friends
    Community = 2,      // Logged-in users
    Public = 3          // Anyone, including anonymous
}

/// <summary>
/// Represents a consent audit log entry
/// </summary>
[Table("consent_audit_logs")]
[Index(nameof(UserId), nameof(ChangedAt))]
public sealed class ConsentAuditLog : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of consent affected
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ConsentType { get; set; } = string.Empty;

    /// <summary>
    /// Action taken (granted, revoked, expired, modified)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Previous state (JSON)
    /// </summary>
    [MaxLength(1000)]
    public string? PreviousState { get; set; }

    /// <summary>
    /// New state (JSON)
    /// </summary>
    [MaxLength(1000)]
    public string? NewState { get; set; }

    /// <summary>
    /// When the change occurred
    /// </summary>
    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which change was made
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context
    /// </summary>
    [MaxLength(1000)]
    public string? Context { get; set; }
}
