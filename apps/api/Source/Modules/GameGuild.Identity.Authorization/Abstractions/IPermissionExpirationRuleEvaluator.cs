namespace GameGuild.Identity.Authorization;

/// <summary>
///     A single advanced-expiration-rule violation (issue #331): which constraint failed and why.
/// </summary>
public sealed record PermissionExpirationRuleViolation(
    string Constraint,
    string Message,
    IReadOnlyList<string> MatchedRuleKeys);

/// <summary>
///     The effective advanced expiration rule resolved for a set of permissions, plus the
/// configuration keys that contributed to it (for administrative introspection).
/// </summary>
public sealed record EffectivePermissionExpirationRule(
    PermissionExpirationRule Rule,
    IReadOnlyList<string> MatchedRuleKeys);

/// <summary>
///     Evaluates advanced expiration rules and conditions (issue #331) against proposed
///     expiration mutations. Pure: no I/O, deterministic for the same options + inputs.
/// </summary>
public interface IPermissionExpirationRuleEvaluator
{
    /// <summary>
    ///     Resolves the effective rule (global baseline + every matching per-permission key,
    ///     strictest constraint wins) for a grant carrying the given permissions.
    /// </summary>
    EffectivePermissionExpirationRule Describe(IReadOnlyCollection<string>? permissions);

    /// <summary>
    ///     Validates an absolute expiration proposal (issue #331 bulk set / grant path).
    ///     A <c>null</c> <paramref name="expiresAt"/> is checked against
    ///     <c>RequireExpiration</c>; a non-null value is checked against the duration floor/cap
    ///     measured from <paramref name="now"/>.
    /// </summary>
    /// <returns>The violations found (empty when the proposal is allowed).</returns>
    IReadOnlyList<PermissionExpirationRuleViolation> ValidateSetExpiration(
        IReadOnlyCollection<string>? permissions,
        DateTime? expiresAt,
        DateTime now);

    /// <summary>
    ///     Validates a relative extension proposal (issue #331 bulk extend). The extension period
    ///     is checked against <c>MaxExtension</c>; the resulting expiration (baseline max of the
    ///     current expiration and now, plus the extension) is checked against the duration cap
    ///     measured from <paramref name="now"/>.
    /// </summary>
    /// <returns>The violations found (empty when the proposal is allowed).</returns>
    IReadOnlyList<PermissionExpirationRuleViolation> ValidateExtension(
        IReadOnlyCollection<string>? permissions,
        DateTime? currentExpiresAt,
        TimeSpan extension,
        DateTime now);
}
