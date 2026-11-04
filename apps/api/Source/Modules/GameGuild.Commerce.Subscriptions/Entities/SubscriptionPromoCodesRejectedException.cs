namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Thrown when every promo code supplied at checkout was rejected (issue #418 fail-closed
///     promo application, issue #1290 status mapping): the codes expired or were fully
///     consumed between the client's preview and the create/change attempt. Mapped to a 422
///     ProblemDetails by the shared exception middleware, matching the custom-quote and
///     fraud-block precedents — a checkout-time promo rejection is a client-input outcome,
///     not a server fault (the pre-fix InvalidOperationException surfaced as a 500).
/// </summary>
public sealed class SubscriptionPromoCodesRejectedException : GameGuild.DomainException
{
    /// <summary>The plan the rejected codes were quoted against.</summary>
    public Guid PlanId { get; }

    /// <summary>Per-code rejection reasons, as reported by the promo engine.</summary>
    public IReadOnlyList<string> Reasons { get; }

    public SubscriptionPromoCodesRejectedException(Guid planId, IEnumerable<string> reasons)
        : base($"None of the supplied promo codes could be applied to plan {planId}. {string.Join("; ", reasons)}")
    {
        PlanId = planId;
        Reasons = reasons.ToList();
    }
}
