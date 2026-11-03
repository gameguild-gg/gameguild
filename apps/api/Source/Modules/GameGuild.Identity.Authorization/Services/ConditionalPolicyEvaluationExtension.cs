using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Folds the tenant's active conditional policies into effective-permission
///     resolution as a registered <see cref="IPermissionEvaluationExtension"/>
///     (issue #1204: policies exist, are versioned and signed, but were never
///     consulted when permissions resolved).
/// </summary>
/// <remarks>
///     <para>
///         <b>Semantics (veto-only, DENY-WINS):</b> matching <b>deny</b> conditional
///         policies subtract their targeted actions from the accumulated allow set; an
///         unconstrained deny policy (no permission-type filter) subtracts everything
///         this resolution had allowed. <b>Allow</b>-effect conditional policies never
///         mint permissions — identical posture to the ABAC overlay
///         (<see cref="AbacPolicyEvaluationExtension"/>), which keeps running after
///         this extension (<see cref="Order"/> 500) so ABAC remains the last veto.
///     </para>
///     <para>
///         <b>No policy configured:</b> a tenant without active conditional policies
///         contributes nothing — the documented acceptable default
///         ("allow when no policy applies"). The repository is consulted once per
///         resolution for the policy existence check; zero-policy tenants never
///         invoke the evaluator at all.
///     </para>
///     <para>
///         <b>Fail-closed guard (issue #1204):</b> when policies <em>exist</em> but
///         their evaluation fails, the chain must not silently allow. With
///         <c>PermissionEngine:ConditionalPolicy:FailClosedOnEvaluationError</c>
///         (default <c>true</c>) the extension denies every permission of the
///         resolution and logs an error; with the switch off, the failure is logged
///         (error) and the extension contributes nothing — the historical fail-open,
///         kept only as an explicit opt-out.
///     </para>
///     <para>
///         Attribute availability at resolution time is narrower than at request-gate
///         time: subject identity (user/tenant), resource type/id and environment time
///         are always supplied; policies conditioned on attributes only present
///         per-request (IP, device, geo, MFA) do not match here and remain enforced by
///         the request-time gates — the same narrowing the ABAC overlay documents.
///     </para>
/// </remarks>
public sealed class ConditionalPolicyEvaluationExtension(
    IConditionalPolicyRepository conditionalPolicyRepository,
    IConditionalPolicyEvaluator conditionalPolicyEvaluator,
    IOptions<PermissionEngineOptions> engineOptions,
    ILogger<ConditionalPolicyEvaluationExtension> logger) : IPermissionEvaluationExtension
{
    public string Name => "ConditionalPolicyOverlay";

    /// <summary>Runs before the ABAC overlay (500) so ABAC stays the final veto.</summary>
    public int Order => 400;

    public async Task<PermissionEvaluationExtensionResult> EvaluateAsync(
        PermissionEvaluationExtensionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.CurrentAllows.Count == 0)
        {
            // Nothing allowed so far: a deny overlay has nothing to subtract.
            return PermissionEvaluationExtensionResult.None;
        }

        List<ConditionalPolicy> policies;
        try
        {
            policies = await conditionalPolicyRepository
                .GetActivePoliciesAsync(context.TenantId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Policy existence is unknown: treat the tenant as policy-configured so the
            // guard below decides fail-closed vs logged skip — never a silent allow.
            return FailClosedOrLogged(
                context,
                exception,
                denyAllMessage: "Conditional policy lookup failed in tenant {TenantId}; policy existence could not be established. FailClosedOnEvaluationError is enabled: denying all {AllowCount} permission(s) of this resolution (fail closed).",
                skipMessage: "Conditional policy lookup failed in tenant {TenantId}; policy existence could not be established. FailClosedOnEvaluationError is disabled: the resolution continues without conditional contributions (explicit fail-open opt-out).");
        }

        if (policies.Count == 0)
        {
            // No policy configured: allow-by-no-policy is the documented default.
            return PermissionEvaluationExtensionResult.None;
        }

        ConditionalPolicySetEvaluationResult evaluation;
        try
        {
            evaluation = await conditionalPolicyEvaluator
                .EvaluateActionsAsync(
                    context.UserId,
                    context.TenantId,
                    context.ResourceType,
                    Guid.TryParse(context.ResourceId, out var resourceId) ? resourceId : null,
                    context.CurrentAllows.ToArray(),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return FailClosedOrLogged(
                context,
                exception,
                denyAllMessage: "Conditional policy evaluation failed in tenant {TenantId} with {PolicyCount} active policy/policies; the policies were not enforced. FailClosedOnEvaluationError is enabled: denying all {AllowCount} permission(s) of this resolution (fail closed).",
                skipMessage: "Conditional policy evaluation failed in tenant {TenantId} with {PolicyCount} active policy/policies; the policies were not enforced. FailClosedOnEvaluationError is disabled: the resolution continues without conditional contributions (explicit fail-open opt-out).",
                policyCount: policies.Count);
        }

        if (evaluation.DeniedActions.Count == 0)
        {
            return PermissionEvaluationExtensionResult.None;
        }

        var denies = evaluation.ContextWideDenial
            ? context.CurrentAllows.ToArray()
            : evaluation.DeniedActions.ToArray();

        logger.LogWarning(
            "Conditional deny policies subtracted {DenyCount} permission(s) from the effective set of user {UserId} in tenant {TenantId} ({PolicyCount} active conditional policies consulted at resolution, context-wide denial: {ContextWideDenial}).",
            denies.Length,
            LogRedaction.ProtectIdentifier(context.UserId),
            LogRedaction.ProtectIdentifier(context.TenantId),
            evaluation.PolicyCount,
            evaluation.ContextWideDenial);

        return new PermissionEvaluationExtensionResult(
            AdditionalAllows: Array.Empty<string>(),
            AdditionalDenies: denies);
    }

    private PermissionEvaluationExtensionResult FailClosedOrLogged(
        PermissionEvaluationExtensionContext context,
        Exception exception,
        string denyAllMessage,
        string skipMessage,
        int? policyCount = null)
    {
        var tenantId = LogRedaction.ProtectIdentifier(context.TenantId);

        if (engineOptions.Value.ConditionalPolicy.FailClosedOnEvaluationError)
        {
            if (policyCount is { } count)
            {
                logger.LogError(exception, denyAllMessage, tenantId, count, context.CurrentAllows.Count);
            }
            else
            {
                logger.LogError(exception, denyAllMessage, tenantId, context.CurrentAllows.Count);
            }

            return new PermissionEvaluationExtensionResult(
                AdditionalAllows: Array.Empty<string>(),
                AdditionalDenies: context.CurrentAllows.ToArray());
        }

        if (policyCount is { } deniedCount)
        {
            logger.LogError(exception, skipMessage, tenantId, deniedCount);
        }
        else
        {
            logger.LogError(exception, skipMessage, tenantId);
        }

        return PermissionEvaluationExtensionResult.None;
    }
}
