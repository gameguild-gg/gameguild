using GameGuild.Database;

namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a data masking rule enforced by the authorization layer
/// </summary>
public class DataMaskingRule : EntityBase<Guid>
{
    /// <summary>
    /// Tenant ID to which this rule belongs (null for global rules)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Name of the masking rule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the masking rule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Target entity/resource type (e.g., "User", "Order", "Transaction")
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Field name to mask (e.g., "Email", "CreditCard", "SSN")
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Type of masking to apply
    /// </summary>
    public MaskingType MaskingType { get; set; }

    /// <summary>
    /// Custom masking pattern (for PatternMask type)
    /// Example: "***@***.com" for email, "****-****-****-1234" for credit card
    /// </summary>
    public string? MaskingPattern { get; set; }

    /// <summary>
    /// Number of characters to show at start (for PartialMask)
    /// </summary>
    public int? ShowFirst { get; set; }

    /// <summary>
    /// Number of characters to show at end (for PartialMask)
    /// </summary>
    public int? ShowLast { get; set; }

    /// <summary>
    /// Character to use for masking (default: *)
    /// </summary>
    public char MaskCharacter { get; set; } = '*';

    /// <summary>
    /// Roles that can see unmasked data (JSON array of role names)
    /// </summary>
    public string? ExemptRoles { get; set; }

    /// <summary>
    /// Permissions required to see unmasked data (JSON array of permission types)
    /// </summary>
    public string? RequiredPermissions { get; set; }

    /// <summary>
    /// User IDs who are exempt from masking (JSON array)
    /// </summary>
    public string? ExemptUsers { get; set; }

    /// <summary>
    /// Whether the rule is currently active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Priority for rule evaluation (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Conditions under which masking applies (JSON serialized)
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// Whether to log when data is accessed in masked form
    /// </summary>
    public bool LogAccess { get; set; } = true;

    /// <summary>
    /// Whether to log when data is accessed in unmasked form
    /// </summary>
    public bool LogUnmaskedAccess { get; set; } = true;

    /// <summary>
    /// User ID who created this rule
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// User ID who last updated this rule
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Types of data masking
/// </summary>
public enum MaskingType
{
    /// <summary>
    /// Full masking - replace all characters with mask character
    /// </summary>
    FullMask = 1,

    /// <summary>
    /// Partial masking - show first/last N characters, mask the rest
    /// </summary>
    PartialMask = 2,

    /// <summary>
    /// Pattern-based masking - use a specific pattern
    /// </summary>
    PatternMask = 3,

    /// <summary>
    /// Hash the value (one-way, cannot be unmasked)
    /// </summary>
    Hash = 4,

    /// <summary>
    /// Encrypt the value (can be decrypted with proper permissions)
    /// </summary>
    Encrypt = 5,

    /// <summary>
    /// Return null instead of the value
    /// </summary>
    Nullify = 6,

    /// <summary>
    /// Redact entire value (replace with [REDACTED])
    /// </summary>
    Redact = 7,

    /// <summary>
    /// Replace with fake/synthetic data
    /// </summary>
    Tokenize = 8
}

/// <summary>
/// Represents a log entry for data access (masked or unmasked)
/// </summary>
public class DataAccessLog : EntityBase<Guid>
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// User who accessed the data
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Masking rule that was applied (null if no masking)
    /// </summary>
    public Guid? MaskingRuleId { get; set; }

    /// <summary>
    /// Resource type accessed
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Resource ID accessed
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Field name accessed
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Whether data was masked
    /// </summary>
    public bool WasMasked { get; set; }

    /// <summary>
    /// Reason why data was unmasked (if applicable)
    /// </summary>
    public string? UnmaskedReason { get; set; }

    /// <summary>
    /// IP address of the accessor
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp of access
    /// </summary>
    public DateTime AccessedAt { get; set; }

    /// <summary>
    /// Navigation property to masking rule
    /// </summary>
    public DataMaskingRule? MaskingRule { get; set; }
}
