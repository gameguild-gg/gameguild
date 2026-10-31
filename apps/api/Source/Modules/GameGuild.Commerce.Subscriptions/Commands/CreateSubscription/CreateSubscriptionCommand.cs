using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to create a new subscription
/// </summary>
/// <param name="TenantId">Tenant the subscription belongs to.</param>
/// <param name="PlanId">Plan the subscription is created on.</param>
/// <param name="CreatedByUserId">User creating the subscription.</param>
/// <param name="BillingCycle">Billing cycle (Monthly, Yearly, ...).</param>
/// <param name="Amount">Subscription amount.</param>
/// <param name="FulfilledOrderId">Optional fulfilled order that triggered the subscription (Order→Subscription causality).</param>
/// <param name="StartDate">Optional start date; defaults to now.</param>
/// <param name="TrialDays">Optional trial period in days.</param>
/// <param name="PromoCodes">Optional promo codes applied at creation (issue #418).</param>
/// <param name="PromoCodeIdempotencyKey">Optional replay-protection key for the promo application.</param>
/// <param name="ProductId">Optional product context for product-scoped promo codes.</param>
/// <param name="FraudCountry">Optional ISO 3166-1 alpha-2 billing country; feeds the creation-time fraud signals (issue #409).</param>
/// <param name="FraudSourceIpAddress">Optional client IP of the creation attempt; feeds the IP blacklist and GeoIP fraud signals (issue #409).</param>
/// <param name="FraudDeviceFingerprint">Optional device fingerprint; feeds the device blacklist fraud signal (issue #409).</param>
/// <param name="FraudPayerEmail">Optional payer email; feeds the disposable-email and email blacklist fraud signals (issue #409).</param>
/// <param name="FraudCardBin">Optional 6-8 digit card BIN prefix; feeds the high-risk BIN range fraud signal (issue #409).</param>
/// <param name="FraudClientTimezone">Optional claimed IANA timezone; feeds the GeoIP timezone-mismatch fraud signal (issue #409).</param>
[RequiresQuota(ResourceUsageType.Subscriptions, 1, Source = "CreateSubscription")]
public sealed record CreateSubscriptionCommand(
    Guid TenantId,
    Guid PlanId,
    Guid CreatedByUserId,
    BillingCycle BillingCycle,
    decimal Amount,
    Guid? FulfilledOrderId = null,
    DateTime? StartDate = null,
    int? TrialDays = null,
    IReadOnlyList<string>? PromoCodes = null,
    string? PromoCodeIdempotencyKey = null,
    Guid? ProductId = null,
    string? FraudCountry = null,
    string? FraudSourceIpAddress = null,
    string? FraudDeviceFingerprint = null,
    string? FraudPayerEmail = null,
    string? FraudCardBin = null,
    string? FraudClientTimezone = null) : ICommand<Guid>;
