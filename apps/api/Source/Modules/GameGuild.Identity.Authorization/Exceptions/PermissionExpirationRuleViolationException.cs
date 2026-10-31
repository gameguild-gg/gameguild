using GameGuild;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Thrown when a proposed permission expiration violates one or more advanced expiration
///     rules (issue #331). Surfaces as HTTP 422 "Domain Rule Violation" through the shared
///     exception-handling middleware. The whole operation is rejected before any grant is mutated.
/// </summary>
public sealed class PermissionExpirationRuleViolationException : DomainException
{
    /// <summary>
    ///     The individual rule violations, each naming the violated constraint.
    /// </summary>
    public IReadOnlyList<PermissionExpirationRuleViolation> Violations { get; }

    public PermissionExpirationRuleViolationException(IReadOnlyList<PermissionExpirationRuleViolation> violations)
        : base(BuildMessage(violations))
    {
        Violations = violations;
    }

    private static string BuildMessage(IReadOnlyList<PermissionExpirationRuleViolation> violations) =>
        violations.Count == 0
            ? "The requested permission expiration violates the configured expiration rules."
            : "The requested permission expiration violates the configured expiration rules: "
              + string.Join("; ", violations.Select(v => v.Message));
}
