
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for creating a subscription
/// </summary>
/// <param name="TenantId">The tenant ID</param>
/// <param name="PlanId">The subscription plan ID</param>
/// <param name="CreatedByUserId">The user who created the subscription</param>
/// <param name="BillingCycle">The billing cycle (Monthly, Yearly, etc.)</param>
/// <param name="Amount">The subscription amount</param>
/// <param name="Currency">The currency code</param>
/// <param name="FulfilledOrderId">Optional Order ID that triggered this subscription (Economic Model: Order→Subscription causality)</param>
/// <param name="StartDate">Optional start date</param>
/// <param name="TrialDays">Optional trial period in days</param>
/// <param name="PromoCodes">Optional promo codes applied at creation (issue #418)</param>
/// <param name="PromoCodeIdempotencyKey">Optional replay-protection key for the promo application</param>
/// <param name="ProductId">Optional product context for product-scoped promo codes</param>
/// <param name="Country">Optional ISO 3166-1 alpha-2 billing country; feeds the creation-time fraud signals (issue #409)</param>
/// <param name="PayerEmail">Optional payer email; feeds the creation-time disposable-email/blacklist fraud signals (issue #409)</param>
/// <param name="CardBin">Optional 6-8 digit card BIN prefix; feeds the creation-time BIN-range fraud signal (issue #409)</param>
public sealed record CreateSubscriptionRequest(
    Guid TenantId,
    Guid PlanId,
    Guid CreatedByUserId,
    BillingCycle BillingCycle,
    decimal Amount,
    string Currency,
    Guid? FulfilledOrderId = null,
    DateTime? StartDate = null,
    int? TrialDays = null,
    IReadOnlyList<string>? PromoCodes = null,
    string? PromoCodeIdempotencyKey = null,
    Guid? ProductId = null,
    string? Country = null,
    string? PayerEmail = null,
    string? CardBin = null);
