using GameGuild.Identity.Users;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Evaluates the optional password age / rotation policy (issue #1088). Passwords
///     without a recorded change timestamp (legacy rows created before the policy, and
///     OAuth-only accounts without a password) are treated as not expirable so enabling the
///     policy never mass-locks existing users: rotation is enforced from the next password
///     set onward.
/// </summary>
internal static class PasswordAgePolicy
{
    /// <summary>
    ///     Returns the effective maximum age in days for the tenant, or <c>null</c> when the
    ///     policy is disabled or misconfigured for the tenant.
    /// </summary>
    public static int? EffectiveMaxAgeDays(Guid? tenantId, PasswordAgePolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Enabled)
        {
            return null;
        }

        if (tenantId is { } activeTenant &&
            options.TenantMaxAgeDays.TryGetValue(activeTenant.ToString(), out var tenantDays) &&
            tenantDays > 0)
        {
            return tenantDays;
        }

        return options.MaxAgeDays > 0 ? options.MaxAgeDays : null;
    }

    /// <summary>Whether the user's current password is older than the effective maximum age.</summary>
    public static bool IsExpired(User user, Guid? tenantId, PasswordAgePolicyOptions options, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (EffectiveMaxAgeDays(tenantId, options) is not { } maxAgeDays)
        {
            return false;
        }

        // Legacy passwords without a timestamp rotate on their next change, not retroactively.
        if (user.PasswordChangedAt is not { } changedAt)
        {
            return false;
        }

        return utcNow - changedAt > TimeSpan.FromDays(maxAgeDays);
    }
}
