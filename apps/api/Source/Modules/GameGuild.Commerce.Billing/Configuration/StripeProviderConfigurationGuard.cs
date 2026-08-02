using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Hosting;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Enforces the cross-module Stripe identity contract before value-moving services start.
/// </summary>
public static class StripeProviderConfigurationGuard
{
    public static void ThrowIfInvalid(
        StripeGatewayOptions gateway,
        BillingConfiguration billing,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(billing);

        if (IsDevelopmentOrTest(environmentName))
            return;

        var ingress = billing.Stripe;
        var failures = new List<string>();
        if (gateway.UseSimulation)
            failures.Add("Stripe payment simulation is not allowed outside Development and Test environments.");
        if (!string.Equals(gateway.AccountId, ingress.AccountId, StringComparison.Ordinal))
            failures.Add("Payments and Billing must use the same canonical Stripe AccountId.");
        if (gateway.LiveMode != ingress.LiveMode)
            failures.Add("Payments and Billing must use the same Stripe live/test mode.");
        if (!string.Equals(gateway.ConnectedAccountId, ingress.ConnectedAccountId, StringComparison.Ordinal))
            failures.Add("Payments and Billing must use the same Stripe ConnectedAccountId.");

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"Stripe provider configuration is inconsistent: {string.Join(' ', failures)}");
        }
    }

    private static bool IsDevelopmentOrTest(string environmentName) =>
        string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
