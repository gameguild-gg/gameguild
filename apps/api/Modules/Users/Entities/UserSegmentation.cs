namespace GameGuild.Modules.Users;

/// <summary>
/// Represents a user tag for segmentation and categorization
/// </summary>
[Table("user_tags")]
[Index(nameof(UserId), nameof(TagName), IsUnique = true)]
public sealed class UserTag : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>
    /// Tag name (e.g., "premium", "beta-tester", "high-value")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Tag category for organization (e.g., "membership", "behavior", "engagement")
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Tag value (optional, for tags with values like "engagement_score:85")
    /// </summary>
    [MaxLength(200)]
    public string? Value { get; set; }

    /// <summary>
    /// When this tag expires (auto-removed after expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Source of the tag (e.g., "manual", "automated", "ml-model")
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = "manual";

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    [MaxLength(1000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Check if tag is expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
}

/// <summary>
/// Represents a user segment definition with dynamic rules
/// </summary>
[Table("user_segments")]
public sealed class UserSegment : EntityBase
{
    /// <summary>
    /// Segment name (e.g., "Active Users", "At Risk")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Segment description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Segment rules in JSON format (for dynamic evaluation)
    /// Example: {"and": [{"field": "lastSeenAt", "operator": ">=", "value": "30d"}]}
    /// </summary>
    [Required]
    public string Rules { get; set; } = "{}";

    /// <summary>
    /// Whether this segment is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Segment type
    /// </summary>
    public SegmentType Type { get; set; } = SegmentType.Dynamic;

    /// <summary>
    /// Last time segment membership was calculated
    /// </summary>
    public DateTime? LastCalculatedAt { get; set; }

    /// <summary>
    /// Number of users currently in this segment
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// How often to recalculate segment membership (in minutes)
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Segment type
/// </summary>
public enum SegmentType
{
    Dynamic = 0,    // Automatically calculated based on rules
    Static = 1,     // Manually assigned
    Smart = 2       // ML-based segmentation
}

/// <summary>
/// Represents membership in a user cohort
/// </summary>
[Table("user_cohorts")]
[Index(nameof(UserId), nameof(CohortName))]
public sealed class UserCohort : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>
    /// Cohort name (e.g., "Q1-2025-Signups", "Mobile-First-Users")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CohortName { get; set; } = string.Empty;

    /// <summary>
    /// When user joined this cohort
    /// </summary>
    [Required]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cohort type
    /// </summary>
    public CohortType Type { get; set; } = CohortType.Behavioral;

    /// <summary>
    /// Additional cohort metadata
    /// </summary>
    [MaxLength(1000)]
    public string? Metadata { get; set; }
}

/// <summary>
/// Cohort type
/// </summary>
public enum CohortType
{
    Temporal = 0,      // Time-based cohorts (signup date, etc.)
    Behavioral = 1,    // Behavior-based cohorts
    Demographic = 2,   // Demographics-based cohorts
    Engagement = 3,    // Engagement level cohorts
    Custom = 4         // Custom cohorts
}
