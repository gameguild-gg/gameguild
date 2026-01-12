
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Rule that requires the user to have completed MFA verification.
///     Parameters:
///     - mfaClaimType: string - The claim type that indicates MFA completion (default: "amr")
///     - mfaClaimValue: string - The value that indicates MFA completion (default: "mfa")
///     - allowMfaClaim: string - Alternative claim type for MFA (e.g., "mfa_verified")
///     - requireRecent: bool - Whether to require recent MFA (within maxAge minutes)
///     - maxAgeMinutes: int - Maximum age of MFA verification in minutes (default: 30)
/// </summary>
public sealed class RequireMfaRuleEvaluator : IRuleEvaluator
{
    public string RuleType => RuleTypes.RequireMfa;

    public Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Task.FromResult(RuleEvaluationResult.Fail("User must be authenticated for MFA check"));
        }

        // Check primary MFA claim (AMR - Authentication Method Reference)
        var mfaClaimType = parameters.GetString("mfaClaimType") ?? "amr";
        var mfaClaimValue = parameters.GetString("mfaClaimValue") ?? "mfa";

        var amrClaim = user.FindFirst(mfaClaimType);
        var hasMfaViaAmr = amrClaim?.Value.Equals(mfaClaimValue, StringComparison.OrdinalIgnoreCase) ?? false;

        // Check alternative MFA claim (e.g., "mfa_verified": "true")
        var allowMfaClaim = parameters.GetString("allowMfaClaim") ?? "mfa_verified";
        var mfaVerifiedClaim = user.FindFirst(allowMfaClaim);
        var hasMfaViaAlt = mfaVerifiedClaim?.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        if (!hasMfaViaAmr && !hasMfaViaAlt)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                "Multi-factor authentication is required for this operation"));
        }

        // Check MFA timestamp if required
        // ReSharper disable once ArgumentsStyleOther - Explicit default value for API clarity
        var requireRecent = parameters.GetBool("requireRecent", defaultValue: false);
        if (requireRecent)
        {
            var maxAgeMinutes = parameters.GetInt("maxAgeMinutes", 30);
            var mfaTimestampClaim = user.FindFirst("mfa_time") ?? user.FindFirst("mfa_timestamp");

            if (mfaTimestampClaim is null)
            {
                // No timestamp claim - consider MFA as stale
                return Task.FromResult(RuleEvaluationResult.Fail(
                    "Recent MFA verification is required but timestamp is not available"));
            }

            // Try to parse as Unix timestamp or ISO 8601
            DateTime mfaTime;
            if (long.TryParse(mfaTimestampClaim.Value, out var unixTimestamp))
            {
                mfaTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            }
            else if (DateTime.TryParse(mfaTimestampClaim.Value, out var parsedTime))
            {
                mfaTime = parsedTime.ToUniversalTime();
            }
            else
            {
                return Task.FromResult(RuleEvaluationResult.Fail(
                    "Could not parse MFA timestamp"));
            }

            var age = DateTime.UtcNow - mfaTime;
            if (age.TotalMinutes > maxAgeMinutes)
            {
                return Task.FromResult(RuleEvaluationResult.Fail(
                    $"MFA verification has expired ({(int)age.TotalMinutes} minutes ago, max allowed: {maxAgeMinutes} minutes)"));
            }
        }

        return Task.FromResult(RuleEvaluationResult.Success());
    }
}
