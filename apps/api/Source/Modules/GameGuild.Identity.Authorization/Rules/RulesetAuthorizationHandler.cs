
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Single authorization handler that evaluates all rules in a policy ruleset.
///     This replaces the need for multiple individual handlers per policy.
/// </summary>
public sealed class RulesetAuthorizationHandler : AuthorizationHandler<RulesetRequirement>
{
    private readonly ILogger<RulesetAuthorizationHandler> _logger;
    private readonly IRuleEvaluatorRegistry _ruleRegistry;
    private readonly IScopedRuleEvaluatorFactory _scopedEvaluatorFactory;
    private readonly IRulesetProvider _rulesetProvider;

    public RulesetAuthorizationHandler(
        IRulesetProvider rulesetProvider,
        IRuleEvaluatorRegistry ruleRegistry,
        IScopedRuleEvaluatorFactory scopedEvaluatorFactory,
        ILogger<RulesetAuthorizationHandler> logger)
    {
        _rulesetProvider = rulesetProvider;
        _ruleRegistry = ruleRegistry;
        _scopedEvaluatorFactory = scopedEvaluatorFactory;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RulesetRequirement requirement)
    {
        var policyName = requirement.PolicyName;

        _logger.LogDebug("Evaluating policy ruleset: {PolicyName}", policyName);

        // Use pre-loaded ruleset if available (avoids double DB load)
        var ruleset = requirement.Ruleset
            ?? await _rulesetProvider.GetRulesetAsync(policyName, CancellationToken.None).ConfigureAwait(false);

        if (ruleset is null)
        {
            _logger.LogWarning("No ruleset found for policy: {PolicyName}", policyName);
            context.Fail(new AuthorizationFailureReason(this, $"Policy '{policyName}' not found"));
            return;
        }

        if (!ruleset.IsActive)
        {
            _logger.LogWarning("Ruleset for policy {PolicyName} is inactive", policyName);
            context.Fail(new AuthorizationFailureReason(this, $"Policy '{policyName}' is disabled"));
            return;
        }

        // Check authentication requirement
        if (ruleset.RequireAuthentication && !(context.User.Identity?.IsAuthenticated ?? false))
        {
            _logger.LogDebug("Policy {PolicyName} requires authentication but user is not authenticated",
                policyName);
            context.Fail(new AuthorizationFailureReason(this, "Authentication required"));
            return;
        }

        // Evaluate each rule in order
        var enabledRules = ruleset.Rules.Where(r => r.Enabled).ToList();

        foreach (var rule in enabledRules)
        {
            // Validate rule before evaluating
            var validation = rule.Validate();
            if (!validation.IsValid)
            {
                _logger.LogError(
                    "Invalid rule configuration in policy {PolicyName}: {Errors}",
                    policyName, string.Join("; ", validation.Errors));
                context.Fail(new AuthorizationFailureReason(
                    this, $"Invalid rule configuration: {string.Join("; ", validation.Errors)}"));
                return;
            }

            var evaluator = ResolveEvaluator(rule.Type);
            if (evaluator is null)
            {
                _logger.LogError(
                    "No evaluator found for rule type: {RuleType} in policy {PolicyName}",
                    rule.Type, policyName);
                context.Fail(new AuthorizationFailureReason(
                    this, $"Unknown rule type: {rule.Type}"));
                return;
            }

            var parameters = RuleParameters.FromDictionary(rule.Params);

            try
            {
                var result = await evaluator.EvaluateAsync(context, parameters).ConfigureAwait(false);

                if (!result.IsSuccess && !result.IsSkipped)
                {
                    _logger.LogDebug(
                        "Rule {RuleType} failed for policy {PolicyName}: {Reason}",
                        rule.Type, policyName, result.FailureReason);
                    context.Fail(new AuthorizationFailureReason(
                        this, result.FailureReason ?? $"Rule '{rule.Type}' failed"));
                    return;
                }

                if (result.IsSkipped)
                {
                    _logger.LogDebug(
                        "Rule {RuleType} skipped for policy {PolicyName}: {Reason}",
                        rule.Type, policyName, result.FailureReason);
                    // Continue to next rule
                }

                // Success - continue to next rule
                _logger.LogDebug("Rule {RuleType} passed for policy {PolicyName}", rule.Type, policyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error evaluating rule {RuleType} for policy {PolicyName}",
                    rule.Type, policyName);
                context.Fail(new AuthorizationFailureReason(
                    this, $"Error evaluating rule '{rule.Type}'"));
                return;
            }
        }

        // All rules passed
        _logger.LogDebug("All rules passed for policy {PolicyName}", policyName);
        context.Succeed(requirement);
    }

    private IRuleEvaluator? ResolveEvaluator(string ruleType)
    {
        // Try to get from registry first (stateless singleton evaluators)
        var evaluator = _ruleRegistry.GetEvaluator(ruleType);
        if (evaluator is not null)
        {
            return evaluator;
        }

        // Try to resolve from scoped factory (scoped evaluators with per-request dependencies)
        return _scopedEvaluatorFactory.GetEvaluator(ruleType);
    }
}
