namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Thrown when subscription creation is refused by the fraud gate at Block level
///     (issue #409: subscription-creation-time risk scoring). Mapped to a 422 ProblemDetails
///     by the shared exception middleware, matching the custom-quote precedent.
/// </summary>
public sealed class SubscriptionFraudBlockedException : GameGuild.DomainException
{
    /// <summary>The tenant whose creation attempt was refused.</summary>
    public Guid TenantId { get; }

    /// <summary>The plan the refused creation targeted.</summary>
    public Guid PlanId { get; }

    /// <summary>Aggregate fraud score behind the refusal.</summary>
    public int Score { get; }

    public SubscriptionFraudBlockedException(Guid tenantId, Guid planId, int score)
        : base($"Subscription creation for plan {planId} was blocked by the fraud gate (score {score}).")
    {
        TenantId = tenantId;
        PlanId = planId;
        Score = score;
    }
}
