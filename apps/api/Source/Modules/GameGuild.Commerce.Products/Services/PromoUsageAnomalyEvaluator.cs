using GameGuild.Compliance.Audit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Products;

/// <summary>Kind of promo usage velocity anomaly (issue #418 unusual-usage alerting).</summary>
public enum PromoUsageAnomalyKind
{
    /// <summary>Validation attempts from one subject exceeded the burst threshold.</summary>
    ValidationBurst = 1,

    /// <summary>Recorded redemptions by one user exceeded the burst threshold.</summary>
    RedemptionBurst = 2
}

/// <summary>
/// Evaluates promo usage velocity for unusual patterns (issue #418). Counts are taken
/// from the promo engine's own attempt/usage tables; when a threshold is breached the
/// evaluator publishes a <c>PromoUsageAnomalyDetected</c> security event through the
/// central <see cref="ISecurityEventLogger"/> pipeline, where the
/// <c>SecurityAlertRuleEvaluator</c> raises (and deduplicates) the
/// <c>security.promo-usage-anomaly</c> alert. Publishing is best-effort: an alerting
/// failure is logged and never propagates to the redemption path.
/// </summary>
public interface IPromoUsageAnomalyEvaluator
{
    /// <summary>
    /// Velocity check after a validation attempt was recorded. The current attempt
    /// must already be persisted (or counted) by the caller.
    /// </summary>
    /// <param name="normalizedCode">The normalized code string (used as the subject for anonymous callers).</param>
    /// <param name="userId">Authenticated user, when present.</param>
    /// <param name="recentAttemptCount">Attempts inside the window including the current one.</param>
    Task EvaluateValidationVelocityAsync(string normalizedCode, Guid? userId, int recentAttemptCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Velocity check after a redemption was recorded. The current usage must
    /// already be persisted.
    /// </summary>
    /// <param name="userId">Redeeming user.</param>
    /// <param name="code">The redeemed promo code string.</param>
    /// <param name="recentUsageCount">Usages inside the window including the current one.</param>
    Task EvaluateRedemptionVelocityAsync(Guid userId, string code, int recentUsageCount, CancellationToken cancellationToken = default);
}

/// <summary>Default evaluator; thresholds come from <see cref="PromoAbuseDetectionOptions"/>.</summary>
public sealed class PromoUsageAnomalyEvaluator(
    ISecurityEventLogger securityEventLogger,
    IOptions<PromoAbuseDetectionOptions> options,
    ILogger<PromoUsageAnomalyEvaluator> logger) : IPromoUsageAnomalyEvaluator
{
    /// <inheritdoc />
    public Task EvaluateValidationVelocityAsync(
        string normalizedCode,
        Guid? userId,
        int recentAttemptCount,
        CancellationToken cancellationToken = default)
    {
        var threshold = Math.Max(2, options.Value.ValidationBurstThreshold);
        if (recentAttemptCount < threshold)
        {
            return Task.CompletedTask;
        }

        return PublishAsync(
            PromoUsageAnomalyKind.ValidationBurst,
            userId,
            normalizedCode,
            $"{recentAttemptCount} promo code validation attempts within {options.Value.ValidationWindowMinutes} minutes (threshold {threshold}).",
            new Dictionary<string, object?>
            {
                ["code"] = normalizedCode,
                ["attemptCount"] = recentAttemptCount,
                ["windowMinutes"] = options.Value.ValidationWindowMinutes
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task EvaluateRedemptionVelocityAsync(
        Guid userId,
        string code,
        int recentUsageCount,
        CancellationToken cancellationToken = default)
    {
        var threshold = Math.Max(2, options.Value.RedemptionBurstThreshold);
        if (recentUsageCount < threshold)
        {
            return Task.CompletedTask;
        }

        return PublishAsync(
            PromoUsageAnomalyKind.RedemptionBurst,
            userId,
            code,
            $"{recentUsageCount} promo code redemptions within {options.Value.RedemptionWindowMinutes} minutes (threshold {threshold}).",
            new Dictionary<string, object?>
            {
                ["code"] = code,
                ["redemptionCount"] = recentUsageCount,
                ["windowMinutes"] = options.Value.RedemptionWindowMinutes
            },
            cancellationToken);
    }

    private async Task PublishAsync(
        PromoUsageAnomalyKind kind,
        Guid? userId,
        string code,
        string detail,
        Dictionary<string, object?> metadata,
        CancellationToken cancellationToken)
    {
        var request = new CreateAuditLogRequest
        {
            ActionType = AuditActionTypes.PromoUsageAnomalyDetected,
            ResourceType = "PromoCode",
            ResourceId = code,
            Category = AuditCategory.Security,
            RiskLevel = AuditRiskLevel.High,
            Success = false,
            ErrorMessage = detail,
            Description = $"Unusual promo code usage pattern detected ({kind}).",
            Metadata = metadata
        };
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            request.UserId = userId.Value;
        }

        try
        {
            await securityEventLogger.RecordAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best effort: anomaly alerting must never change the redemption outcome.
            logger.LogError(
                exception,
                "Failed to publish promo usage anomaly event ({Kind}) for code {Code}",
                kind,
                code);
        }
    }
}
