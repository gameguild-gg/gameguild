using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified implementation of the policy gate service.
///     Evaluates all deny-focused gates (Conditional, ABAC, Environment).
///     Uses DENY-WINS precedence.
/// </summary>
public class PolicyGateService(
    IConditionalPolicyEvaluator conditionalEvaluator,
    IAbacPolicyEvaluator abacEvaluator,
    ILogger<PolicyGateService> logger
) : IPolicyGateService
{
    /// <summary>
    ///     Hard-coded gates that cannot be bypassed.
    /// </summary>
    private static readonly List<Func<PolicyGateContext, GateEvaluationDetail?>> StaticGates =
    [
        // Block all requests from localhost in production (example)
        context =>
        {
            // This is a placeholder - in real deployment, check environment
            if (context.IpAddress == "127.0.0.1" && context.Attributes.TryGetValue("environment", out var env) && env == "production")
            {
                return new GateEvaluationDetail(
                    PolicyGateType.Static,
                    null,
                    "LocalhostProductionBlock",
                    false,
                    "Localhost access blocked in production");
            }
            return null;
        },
        
        // Block requests with no user agent (bots, scripts)
        context =>
        {
            if (string.IsNullOrEmpty(context.UserAgent) && context.Attributes.TryGetValue("require-user-agent", out var required) && required == "true")
            {
                return new GateEvaluationDetail(
                    PolicyGateType.Static,
                    null,
                    "UserAgentRequired",
                    false,
                    "User-Agent header is required");
            }
            return null;
        }
    ];

    public async Task<PolicyGateResult> EvaluateGatesAsync(
        PolicyGateContext context,
        CancellationToken ct = default)
    {
        var details = new List<GateEvaluationDetail>();

        // 1. Evaluate static hard-coded gates first (highest priority)
        foreach (var gate in StaticGates)
        {
            var result = gate(context);
            if (result != null)
            {
                details.Add(result);
                if (!result.Passed)
                {
                    logger.LogWarning(
                        "Static gate {GateName} denied access for actor {ActorId}",
                        result.GateName, context.ActorId);

                    return new PolicyGateResult(
                        IsAllowed: false,
                        DeniedByGate: PolicyGateType.Static,
                        DeniedByPolicyId: null,
                        DenialReason: result.DenialReason,
                        GateDetails: details);
                }
            }
        }

        // 2. Evaluate conditional policies (time windows, environment conditions)
        var conditionalContext = new ConditionalPolicyContext(
            UserId: context.ActorId,
            TenantId: context.TenantId,
            ResourceType: context.ResourceType,
            ResourceId: context.ResourceId,
            Action: context.Action,
            UserRoles: context.Attributes.TryGetValue("roles", out var roles) && roles is IReadOnlyList<string> roleList 
                ? roleList 
                : [],
            IpAddress: context.IpAddress,
            UserAgent: context.UserAgent,
            DeviceFingerprint: context.DeviceFingerprint,
            GeoCountry: context.GeoLocation?.Split('/').FirstOrDefault(),
            GeoRegion: context.GeoLocation?.Contains('/') == true ? context.GeoLocation.Split('/').LastOrDefault() : null,
            AuthenticationTime: context.Attributes.TryGetValue("auth-time", out var authTime) && authTime is DateTime at ? at : null,
            IsMfaVerified: context.Attributes.TryGetValue("mfa-verified", out var mfa) && mfa is bool m ? m : null,
            RiskScore: context.Attributes.TryGetValue("risk-score", out var risk) && risk is int r ? r : null,
            CustomAttributes: context.Attributes
                .Where(kv => kv.Value is string)
                .ToDictionary(kv => kv.Key, kv => (string)kv.Value));

        var conditionalResult = await conditionalEvaluator.EvaluateAsync(conditionalContext, ct);

        details.Add(new GateEvaluationDetail(
            PolicyGateType.Conditional,
            conditionalResult.DeniedByPolicyId,
            conditionalResult.DeniedByPolicyName ?? "ConditionalPolicies",
            conditionalResult.IsAllowed,
            conditionalResult.DenialReason));

        if (!conditionalResult.IsAllowed)
        {
            logger.LogWarning(
                "Conditional policy gate denied access for actor {ActorId}: {Reason}",
                context.ActorId, conditionalResult.DenialReason);

            return new PolicyGateResult(
                IsAllowed: false,
                DeniedByGate: PolicyGateType.Conditional,
                DeniedByPolicyId: conditionalResult.DeniedByPolicyId,
                DenialReason: conditionalResult.DenialReason,
                GateDetails: details);
        }

        // 3. Evaluate ABAC policies
        var abacContext = new AbacRequestContextBuilder()
            .WithSubject(
                context.ActorId,
                context.TenantId,
                context.Attributes.TryGetValue("roles", out var r) && r is IEnumerable<string> rs ? rs : [])
            .WithResource(context.ResourceType, context.ResourceId)
            .WithAction(context.Action)
            .WithEnvironment(context.IpAddress, context.UserAgent, context.GeoLocation?.Split('/').FirstOrDefault())
            .Build();

        // Add custom attributes
        foreach (var attr in context.Attributes)
        {
            // Subject attributes
            if (attr.Key.StartsWith("subject.", StringComparison.OrdinalIgnoreCase))
            {
                abacContext = abacContext with
                {
                    SubjectAttributes = new Dictionary<string, object>(abacContext.SubjectAttributes)
                    {
                        [attr.Key] = attr.Value
                    }
                };
            }
        }

        var abacResult = await abacEvaluator.EvaluateAsync(abacContext, ct);

        details.Add(new GateEvaluationDetail(
            PolicyGateType.Abac,
            abacResult.DecidingPolicyId,
            abacResult.DecidingPolicyName ?? "ABACPolicies",
            abacResult.Decision != AbacDecision.Deny,
            abacResult.DenialReason));

        if (abacResult.Decision == AbacDecision.Deny)
        {
            logger.LogWarning(
                "ABAC gate denied access for actor {ActorId}: {Reason}",
                context.ActorId, abacResult.DenialReason);

            return new PolicyGateResult(
                IsAllowed: false,
                DeniedByGate: PolicyGateType.Abac,
                DeniedByPolicyId: abacResult.DecidingPolicyId,
                DenialReason: abacResult.DenialReason,
                GateDetails: details);
        }

        // All gates passed
        logger.LogDebug("All policy gates passed for actor {ActorId}", context.ActorId);

        return new PolicyGateResult(
            IsAllowed: true,
            GateDetails: details);
    }

    public async Task<PolicyGateResult> EvaluateGateAsync(
        PolicyGateType gateType,
        PolicyGateContext context,
        CancellationToken ct = default)
    {
        return gateType switch
        {
            PolicyGateType.Static => EvaluateStaticGates(context),
            PolicyGateType.Conditional => await EvaluateConditionalGateAsync(context, ct),
            PolicyGateType.Abac => await EvaluateAbacGateAsync(context, ct),
            PolicyGateType.Environment => EvaluateEnvironmentGate(context),
            _ => new PolicyGateResult(IsAllowed: true)
        };
    }

    private PolicyGateResult EvaluateStaticGates(PolicyGateContext context)
    {
        var details = new List<GateEvaluationDetail>();
        
        foreach (var gate in StaticGates)
        {
            var result = gate(context);
            if (result != null)
            {
                details.Add(result);
                if (!result.Passed)
                {
                    return new PolicyGateResult(
                        IsAllowed: false,
                        DeniedByGate: PolicyGateType.Static,
                        DenialReason: result.DenialReason,
                        GateDetails: details);
                }
            }
        }

        return new PolicyGateResult(IsAllowed: true, GateDetails: details);
    }

    private async Task<PolicyGateResult> EvaluateConditionalGateAsync(
        PolicyGateContext context,
        CancellationToken ct)
    {
        var conditionalContext = new ConditionalPolicyContext(
            UserId: context.ActorId,
            TenantId: context.TenantId,
            ResourceType: context.ResourceType,
            ResourceId: context.ResourceId,
            Action: context.Action,
            UserRoles: []);

        var result = await conditionalEvaluator.EvaluateAsync(conditionalContext, ct);

        return new PolicyGateResult(
            IsAllowed: result.IsAllowed,
            DeniedByGate: result.IsAllowed ? null : PolicyGateType.Conditional,
            DeniedByPolicyId: result.DeniedByPolicyId,
            DenialReason: result.DenialReason);
    }

    private async Task<PolicyGateResult> EvaluateAbacGateAsync(
        PolicyGateContext context,
        CancellationToken ct)
    {
        var abacContext = new AbacRequestContextBuilder()
            .WithSubject(context.ActorId, context.TenantId, [])
            .WithResource(context.ResourceType, context.ResourceId)
            .WithAction(context.Action)
            .Build();

        var result = await abacEvaluator.EvaluateAsync(abacContext, ct);

        return new PolicyGateResult(
            IsAllowed: result.Decision != AbacDecision.Deny,
            DeniedByGate: result.Decision == AbacDecision.Deny ? PolicyGateType.Abac : null,
            DeniedByPolicyId: result.DecidingPolicyId,
            DenialReason: result.DenialReason);
    }

    private PolicyGateResult EvaluateEnvironmentGate(PolicyGateContext context)
    {
        // Basic environment checks (can be extended)
        var details = new List<GateEvaluationDetail>();

        // Check for suspicious patterns
        if (!string.IsNullOrEmpty(context.UserAgent) && 
            (context.UserAgent.Contains("curl", StringComparison.OrdinalIgnoreCase) ||
             context.UserAgent.Contains("wget", StringComparison.OrdinalIgnoreCase)))
        {
            // Log but don't deny - could be legitimate API usage
            details.Add(new GateEvaluationDetail(
                PolicyGateType.Environment,
                null,
                "CommandLineToolDetected",
                true,
                "Command-line tool detected but allowed"));
        }

        return new PolicyGateResult(IsAllowed: true, GateDetails: details);
    }
}
