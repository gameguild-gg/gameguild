using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Requires the authenticated user to hold at least one configured role.
/// </summary>
public sealed class RequireAnyRoleRuleEvaluator : IRuleEvaluator
{
    public string RuleType => RuleTypes.RequireAnyRole;

    public Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            return Task.FromResult(RuleEvaluationResult.Fail("User is not authenticated"));
        }

        var requiredRoles = parameters.GetStringArray("roles");
        if (requiredRoles.Count == 0)
        {
            return Task.FromResult(RuleEvaluationResult.Fail("At least one role is required"));
        }

        var userRoles = Utilities.ClaimsExtractor.GetRoles(context.User);
        return Task.FromResult(requiredRoles.Any(userRoles.Contains)
            ? RuleEvaluationResult.Success()
            : RuleEvaluationResult.Fail($"User does not have any required roles: {string.Join(", ", requiredRoles)}"));
    }
}
