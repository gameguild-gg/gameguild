using System.Text.Json.Serialization;

namespace GameGuild.Identity.Users;

/// <summary>
///     Value object representing user account status with state machine semantics.
///     Encapsulates IsActive/IsSuspended logic and enforces valid state transitions.
/// </summary>
/// <remarks>
///     State Machine:
///     <code>
///         Active
///           ↓ Deactivate()        ↓ Suspend()
///        Inactive              Suspended
///           ↓ Activate()          ↓ Unsuspend()
///         Active                Active
///     </code>
///     Note: A suspended user remains active (can be reactivated without unsuspending).
///     Suspension is a temporary restriction, deactivation is a permanent state change.
/// </remarks>
public readonly record struct UserStatus
{
    /// <summary>
    ///     Whether the user account is active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    ///     Whether the user account is suspended
    /// </summary>
    public bool IsSuspended { get; init; }

    /// <summary>
    ///     Creates a new UserStatus
    /// </summary>
    [JsonConstructor]
    public UserStatus(bool isActive, bool isSuspended)
    {
        IsActive = isActive;
        IsSuspended = isSuspended;
    }

    // ========================
    // FACTORY METHODS
    // ========================

    /// <summary>
    ///     Creates an active, non-suspended status (default for new users)
    /// </summary>
    public static UserStatus Active() => new(isActive: true, isSuspended: false);

    /// <summary>
    ///     Creates an inactive, non-suspended status
    /// </summary>
    public static UserStatus Inactive() => new(isActive: false, isSuspended: false);

    /// <summary>
    ///     Creates a suspended status (still active until explicitly deactivated)
    /// </summary>
    public static UserStatus Suspended() => new(isActive: true, isSuspended: true);

    // ========================
    // STATE QUERIES
    // ========================

    /// <summary>
    ///     Returns true if the user can perform actions (active AND not suspended)
    /// </summary>
    [JsonIgnore]
    public bool CanPerformActions => IsActive && !IsSuspended;

    /// <summary>
    ///     Returns true if the user can sign in (active, even if suspended for warning)
    /// </summary>
    [JsonIgnore]
    public bool CanSignIn => IsActive;

    /// <summary>
    ///     Gets the current status as a human-readable string
    /// </summary>
    [JsonIgnore]
    public string StatusName => (IsActive, IsSuspended) switch
    {
        (true, false) => "Active",
        (true, true) => "Suspended",
        (false, false) => "Inactive",
        (false, true) => "Inactive (Suspended)"
    };

    // ========================
    // STATE TRANSITIONS
    // ========================

    /// <summary>
    ///     Activates the user account
    /// </summary>
    public UserStatus Activate() => this with { IsActive = true };

    /// <summary>
    ///     Deactivates the user account
    /// </summary>
    public UserStatus Deactivate() => this with { IsActive = false };

    /// <summary>
    ///     Suspends the user account
    /// </summary>
    public UserStatus Suspend() => this with { IsSuspended = true };

    /// <summary>
    ///     Removes suspension from the user account
    /// </summary>
    public UserStatus Unsuspend() => this with { IsSuspended = false };

    public override string ToString() => StatusName;
}
