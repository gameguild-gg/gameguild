namespace GameGuild.Modules.Users.Entities;

/// <summary>
/// Represents a detected compromised credential (password breach detection).
/// Tracks credentials that have been found in data breaches and require user action.
/// </summary>
public class CompromisedCredential : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the user whose credential was compromised.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SHA-256 hash of the compromised credential.
    /// Stored as hash for security (never store actual passwords).
    /// </summary>
    public string CredentialHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of credential (Password, PIN, SecurityAnswer).
    /// </summary>
    public string CredentialType { get; set; } = "Password";

    /// <summary>
    /// Gets or sets when the credential was detected as compromised.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Gets or sets the source of the breach information (HIBP, InternalScan, ManualReport).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the breach name/identifier from the source.
    /// </summary>
    public string? BreachName { get; set; }

    /// <summary>
    /// Gets or sets the date when the breach occurred (if known).
    /// </summary>
    public DateTime? BreachDate { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the compromise.
    /// </summary>
    public BreachSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the number of times this credential has been seen in breaches.
    /// Higher count = more widely compromised.
    /// </summary>
    public int BreachCount { get; set; }

    /// <summary>
    /// Gets or sets the status of the compromised credential.
    /// </summary>
    public CompromiseStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the user was notified about this compromise.
    /// </summary>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>
    /// Gets or sets when the user acknowledged the notification.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Gets or sets when the credential was resolved (password changed).
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the breach (JSON format).
    /// Can include breach description, affected data types, etc.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the resolution action taken by the user.
    /// </summary>
    public string? ResolutionAction { get; set; }

    /// <summary>
    /// Gets whether this credential is still active (not resolved).
    /// </summary>
    public bool IsActive => Status == CompromiseStatus.Active;

    /// <summary>
    /// Gets whether the user needs to take action.
    /// </summary>
    public bool RequiresAction => Status == CompromiseStatus.Active || Status == CompromiseStatus.Acknowledged;

    /// <summary>
    /// Marks the compromise as acknowledged by the user.
    /// </summary>
    public void Acknowledge()
    {
        Status = CompromiseStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the compromise as resolved (credential changed).
    /// </summary>
    public void Resolve(string action)
    {
        Status = CompromiseStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolutionAction = action;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the compromise as ignored by the user.
    /// </summary>
    public void Ignore()
    {
        Status = CompromiseStatus.Ignored;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records that the user was notified about this compromise.
    /// </summary>
    public void RecordNotification()
    {
        NotifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Severity level of a credential breach.
/// </summary>
public enum BreachSeverity
{
    /// <summary>
    /// Low severity - credential found in minor breach.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity - credential found in moderate breach.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity - credential found in major breach.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - credential found in massive breach or multiple breaches.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Status of a compromised credential.
/// </summary>
public enum CompromiseStatus
{
    /// <summary>
    /// Active compromise, user not yet notified or action not taken.
    /// </summary>
    Active = 0,

    /// <summary>
    /// User has been notified and acknowledged the compromise.
    /// </summary>
    Acknowledged = 1,

    /// <summary>
    /// Compromise resolved by changing the credential.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// User has chosen to ignore this compromise.
    /// </summary>
    Ignored = 3,

    /// <summary>
    /// False positive - not actually compromised.
    /// </summary>
    FalsePositive = 4
}

/// <summary>
/// Represents a credential check result from external services like HIBP.
/// Used for caching and tracking check history.
/// </summary>
public class CredentialCheckLog : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the user whose credential was checked.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SHA-256 hash of the checked credential.
    /// </summary>
    public string CredentialHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Gets or sets the service used for checking (HIBP, InternalDB, etc.).
    /// </summary>
    public string CheckService { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the credential was found to be compromised.
    /// </summary>
    public bool IsCompromised { get; set; }

    /// <summary>
    /// Gets or sets the number of times found in breaches (if compromised).
    /// </summary>
    public int BreachCount { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the check was initiated.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets additional check metadata (JSON format).
    /// </summary>
    public string? Metadata { get; set; }
}
