
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Rule that requires the request to be within the user's tenant.
/// </summary>
public sealed class TenantMatchRuleEvaluator : IRuleEvaluator
{
    private readonly IAuthorizationTenantContext _tenantContext;

    public TenantMatchRuleEvaluator(IAuthorizationTenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public string RuleType => RuleTypes.TenantMatch;

    public Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var user = context.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Task.FromResult(RuleEvaluationResult.Fail("User is not authenticated"));
        }

        // Get tenant from user claims using centralized helper
        var userTenantClaim = ClaimNames.GetTenantId(user);

        if (string.IsNullOrEmpty(userTenantClaim))
        {
            return Task.FromResult(RuleEvaluationResult.Fail("User has no tenant claim"));
        }

        // Get current request tenant
        var requestTenantId = _tenantContext.TenantId;

        if (string.IsNullOrEmpty(requestTenantId))
        {
            // No tenant context - allow if parameter says so
            // ReSharper disable once ArgumentsStyleOther - Explicit default value for API clarity
            if (parameters.GetBool("allowNoTenant", defaultValue: false))
            {
                return Task.FromResult(RuleEvaluationResult.Success());
            }

            return Task.FromResult(RuleEvaluationResult.Fail("No tenant context available"));
        }

        // Compare tenants
        if (!string.Equals(userTenantClaim, requestTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                $"Tenant mismatch: user belongs to '{userTenantClaim}' but request is for '{requestTenantId}'"));
        }

        return Task.FromResult(RuleEvaluationResult.Success());
    }
}
