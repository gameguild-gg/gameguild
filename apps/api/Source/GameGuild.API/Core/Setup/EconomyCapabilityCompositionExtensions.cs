using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using GameGuild.Compliance.FinancialCrime;
using GameGuild.Economy.Risk;
using GameGuild.TrustSafety;

namespace GameGuild.API.Setup;

public static class EconomyCapabilityCompositionExtensions
{
    public static IServiceCollection AddEconomyCapabilityComposition(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddEconomyRiskComposition(configuration);
        services.AddFinancialCrimeComposition();
        services.AddTrustSafetyComposition();
        return services;
    }
}

public static class EconomyProviderCapabilityGuard
{
    private static readonly EconomyValueMovementCapability[] ProviderBackedCapabilities =
    [
        EconomyValueMovementCapability.ConfirmHardCoinFunding,
        EconomyValueMovementCapability.ReverseProviderFunding,
        EconomyValueMovementCapability.PayoutExecution,
        EconomyValueMovementCapability.AdminWithdrawalExecution
    ];

    public static void ThrowIfInvalid(
        EconomyRiskCompositionOptions economy,
        StripeGatewayOptions gateway,
        BillingConfiguration billing,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(economy);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(billing);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        EconomyValueMovementCapabilities.Validate(economy);
        var enabledCapabilities = EconomyValueMovementCapabilities.Parse(economy.EnabledCapabilities);
        if (!economy.ValueMovingDecisionsEnabled ||
            !ProviderBackedCapabilities.Any(enabledCapabilities.Contains))
            return;

        var failures = new List<string>();
        if (!gateway.IsEnabled)
            failures.Add("Payments:Stripe:IsEnabled must be true.");
        if (gateway.UseSimulation && !IsDevelopmentOrTest(environmentName))
            failures.Add("Payments:Stripe:UseSimulation must be false outside Development and Test.");
        if (!string.Equals(gateway.AccountId, billing.Stripe.AccountId, StringComparison.Ordinal))
            failures.Add("Payments and Billing must use the same canonical Stripe account.");
        if (gateway.LiveMode != billing.Stripe.LiveMode)
            failures.Add("Payments and Billing must use the same Stripe live/test mode.");
        if (!billing.Webhook.VerifySignatures)
            failures.Add("Billing:Webhook:VerifySignatures must be true.");
        if (!billing.Webhook.StorePayloads)
            failures.Add("Billing:Webhook:StorePayloads must be true for replay-safe ingress.");
        if (string.IsNullOrWhiteSpace(billing.Stripe.WebhookSecret))
            failures.Add("Billing:Stripe:WebhookSecret is required.");
        if (string.IsNullOrWhiteSpace(billing.Stripe.WebhookEndpointId))
            failures.Add("Billing:Stripe:WebhookEndpointId is required.");
        if (string.IsNullOrWhiteSpace(billing.Stripe.ApiVersion))
            failures.Add("Billing:Stripe:ApiVersion is required.");
        if (billing.Stripe.WebhookToleranceSeconds is < 1 or > 900)
            failures.Add("Billing:Stripe:WebhookToleranceSeconds must be between 1 and 900.");
        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase) &&
            (!gateway.LiveMode || !billing.Stripe.LiveMode))
            failures.Add("Stripe live mode is required in Production.");

        if (failures.Count != 0)
            throw new EconomyProviderConfigurationException(string.Join(" ", failures));
    }

    private static bool IsDevelopmentOrTest(string environmentName) =>
        string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}

public sealed class EconomyProviderConfigurationException(string message) : InvalidOperationException(message);
