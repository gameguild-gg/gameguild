using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Default <see cref="IPermissionExpirationRuleEvaluator"/> (issue #331): resolves the
///     effective advanced expiration rule from <see cref="PermissionExpirationOptions"/> and
///     validates proposed set/extend mutations against it. Pure and deterministic.
/// </summary>
public sealed class PermissionExpirationRuleEvaluator(
    IOptions<PermissionExpirationOptions> expirationOptions) : IPermissionExpirationRuleEvaluator
{
    private PermissionExpirationOptions Options => expirationOptions.Value;

    /// <inheritdoc />
    public EffectivePermissionExpirationRule Describe(IReadOnlyCollection<string>? permissions)
    {
        var (rule, matchedKeys) = Options.ResolveEffectiveRule(permissions);
        return new EffectivePermissionExpirationRule(rule, matchedKeys);
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionExpirationRuleViolation> ValidateSetExpiration(
        IReadOnlyCollection<string>? permissions,
        DateTime? expiresAt,
        DateTime now)
    {
        var (rule, matchedKeys) = Options.ResolveEffectiveRule(permissions);
        var violations = new List<PermissionExpirationRuleViolation>();

        if (rule.IsEmpty)
        {
            return violations;
        }

        if (expiresAt is null)
        {
            if (rule.RequireExpiration == true)
            {
                violations.Add(new PermissionExpirationRuleViolation(
                    "RequireExpiration",
                    "Matching permissions must carry an expiration; clearing ExpiresAt is not allowed for this grant.",
                    matchedKeys));
            }

            return violations;
        }

        var duration = expiresAt.Value - now;

        if (rule.MinDuration is { } floor && duration < floor)
        {
            violations.Add(new PermissionExpirationRuleViolation(
                "MinDuration",
                $"Expiration must be at least {floor} away from now (proposed {duration}).",
                matchedKeys));
        }

        if (rule.MaxDuration is { } cap && duration > cap)
        {
            violations.Add(new PermissionExpirationRuleViolation(
                "MaxDuration",
                $"Expiration must be at most {cap} away from now (proposed {duration}).",
                matchedKeys));
        }

        return violations;
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionExpirationRuleViolation> ValidateExtension(
        IReadOnlyCollection<string>? permissions,
        DateTime? currentExpiresAt,
        TimeSpan extension,
        DateTime now)
    {
        var (rule, matchedKeys) = Options.ResolveEffectiveRule(permissions);
        var violations = new List<PermissionExpirationRuleViolation>();

        if (rule.IsEmpty)
        {
            return violations;
        }

        if (rule.MaxExtension is { } maxExtension && extension > maxExtension)
        {
            violations.Add(new PermissionExpirationRuleViolation(
                "MaxExtension",
                $"A single extension may add at most {maxExtension} (requested {extension}).",
                matchedKeys));
        }

        // Mirror ExtendExpirationAsync's baseline semantics: an expired (or absent) current
        // expiration extends from now; a still-active one extends from itself.
        var baseline = currentExpiresAt is { } current && current > now ? current : now;
        var resultingDuration = baseline + extension - now;

        if (rule.MaxDuration is { } cap && resultingDuration > cap)
        {
            violations.Add(new PermissionExpirationRuleViolation(
                "MaxDuration",
                $"The extended expiration must remain at most {cap} away from now (it would be {resultingDuration}).",
                matchedKeys));
        }

        if (rule.MinDuration is { } floor && resultingDuration < floor)
        {
            violations.Add(new PermissionExpirationRuleViolation(
                "MinDuration",
                $"The extended expiration must remain at least {floor} away from now (it would be {resultingDuration}).",
                matchedKeys));
        }

        return violations;
    }
}
