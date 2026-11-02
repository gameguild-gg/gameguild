namespace GameGuild.Identity.Authorization;

/// <summary>
///     An advanced expiration rule (issue #331, "advanced expiration rules and conditions").
///     Constrains what expiration values may be granted to permissions that match the rule's key
///     (an exact permission string or a <c>prefix:*</c> wildcard in
///     <see cref="PermissionExpirationOptions.Rules"/>, plus the global baseline rule).
/// </summary>
/// <remarks>
///     <para>
///         All properties are optional; <c>null</c> means "no constraint". When several rules match
///         the same grant (global baseline + one key per matching permission), the strictest value
///         of each constraint wins: <see cref="RequireExpiration"/> is the logical OR, the duration
///         caps take the minimum, and the duration floors take the maximum.
///     </para>
///     <para>
///         <b>SECURITY:</b> rules are enforced before any mutation is applied — a violating bulk
///         set/extend fails the whole operation with a
///         <see cref="PermissionExpirationRuleViolationException"/> before the first grant is touched.
///     </para>
/// </remarks>
public sealed class PermissionExpirationRule
{
    /// <summary>
    ///     When true, matching permission grants may never be permanent: setting a <c>null</c>
    ///     expiration (clearing <c>ExpiresAt</c>) is rejected, and new grants without a resolvable
    ///     default expiration fail.
    /// </summary>
    public bool? RequireExpiration { get; set; }

    /// <summary>
    ///     Shortest allowed distance (from the moment of the operation) for a newly set expiration.
    ///     Prevents near-instant grants that would expire before they can be used or audited.
    /// </summary>
    public TimeSpan? MinDuration { get; set; }

    /// <summary>
    ///     Longest allowed distance (from the moment of the operation) for any expiration —
    ///     a rolling cap that also applies to the result of an extension.
    /// </summary>
    public TimeSpan? MaxDuration { get; set; }

    /// <summary>
    ///     Longest allowed period for a single bulk extension operation.
    /// </summary>
    public TimeSpan? MaxExtension { get; set; }

    /// <summary>
    ///     Merges <paramref name="other"/> into this rule, keeping the strictest value of every
    ///     constraint (OR for <see cref="RequireExpiration"/>, min for caps, max for floors).
    /// </summary>
    public void MergeWith(PermissionExpirationRule other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (RequireExpiration == true || other.RequireExpiration == true)
        {
            RequireExpiration = true;
        }
        else if (RequireExpiration is not null || other.RequireExpiration is not null)
        {
            RequireExpiration = false;
        }

        MinDuration = MaxOf(MinDuration, other.MinDuration);
        MaxDuration = MinOf(MaxDuration, other.MaxDuration);
        MaxExtension = MinOf(MaxExtension, other.MaxExtension);
    }

    /// <summary>
    ///     True when no constraint is configured (the rule constrains nothing).
    /// </summary>
    public bool IsEmpty =>
        RequireExpiration is null
        && MinDuration is null
        && MaxDuration is null
        && MaxExtension is null;

    private static TimeSpan? MinOf(TimeSpan? left, TimeSpan? right) => (left, right) switch
    {
        (null, null) => null,
        (null, { } r) => r,
        ({ } l, null) => l,
        ({ } l, { } r) => l < r ? l : r
    };

    private static TimeSpan? MaxOf(TimeSpan? left, TimeSpan? right) => (left, right) switch
    {
        (null, null) => null,
        (null, { } r) => r,
        ({ } l, null) => l,
        ({ } l, { } r) => l > r ? l : r
    };
}
