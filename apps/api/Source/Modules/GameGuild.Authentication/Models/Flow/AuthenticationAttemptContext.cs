namespace GameGuild.Authentication.Models.Flow;

/// <summary>
///     Context information for an authentication attempt.
/// </summary>
public abstract class AuthenticationAttemptContext
{
    /// <summary>
    ///     User identifier (email, username, or wallet address) being authenticated.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    ///     Authentication method used (Local, OAuth, Web3, etc.).
    /// </summary>
    public string AuthenticationMethod { get; set; } = string.Empty;

    /// <summary>
    ///     IP address of the attempt.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    ///     User agent string.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    ///     Device information.
    /// </summary>
    public DeviceInfo? Device { get; set; }

    /// <summary>
    ///     Alias for Device property to match AuthService usage.
    /// </summary>
    public DeviceInfo? DeviceInfo { get => Device; set => Device = value; }

    /// <summary>
    ///     Location information.
    /// </summary>
    public LocationInfo? Location { get; set; }

    /// <summary>
    ///     Alias for Location property to match AuthService usage.
    /// </summary>
    public LocationInfo? LocationInfo { get => Location; set => Location = value; }

    /// <summary>
    ///     Device fingerprint.
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    ///     Tenant context.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Alias for tenant context to match AuthService usage.
    /// </summary>
    public TenantInfo? TenantInfo { get; set; }

    /// <summary>
    ///     When the attempt occurred.
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    ///     Time of day (for behavioral analysis).
    /// </summary>
    public TimeSpan TimeOfDay { get => AttemptedAt.TimeOfDay; }

    /// <summary>
    ///     Day of week (for behavioral analysis).
    /// </summary>
    public DayOfWeek DayOfWeek { get => AttemptedAt.DayOfWeek; }

    /// <summary>
    ///     Whether this is a weekend.
    /// </summary>
    public bool IsWeekend { get => DayOfWeek == DayOfWeek.Saturday || DayOfWeek == DayOfWeek.Sunday; }

    /// <summary>
    ///     Additional context metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
