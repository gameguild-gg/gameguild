using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GameGuild.API.HealthChecks;

internal sealed class PaymentProviderReadinessHealthCheck(
    IOptions<StripeGatewayOptions> stripeOptions,
    IOptions<BillingConfiguration> billingOptions,
    IHostEnvironment environment) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = stripeOptions.Value;
            var billing = billingOptions.Value;
            var stripeIngress = billing.Stripe;
            var simulationAllowed = IsDevelopmentOrTest(environment.EnvironmentName);
            var outboundConfigured = !string.IsNullOrWhiteSpace(options.ApiKey) &&
                                     !string.IsNullOrWhiteSpace(options.PublishableKey) &&
                                     (!environment.IsProduction() ||
                                      options.ApiKey.StartsWith("sk_live_", StringComparison.Ordinal) &&
                                      options.PublishableKey.StartsWith("pk_live_", StringComparison.Ordinal));
            var webhookConfigured = !string.IsNullOrWhiteSpace(stripeIngress.WebhookSecret) &&
                                    !string.IsNullOrWhiteSpace(stripeIngress.WebhookEndpointId) &&
                                    !string.IsNullOrWhiteSpace(stripeIngress.AccountId) &&
                                    !string.IsNullOrWhiteSpace(stripeIngress.ApiVersion);
            var signatureVerificationEnabled = billing.Webhook.VerifySignatures;
            var providerEnvironment = stripeIngress.LiveMode ? "live" : "test";
            var providerEnvironmentReady = !environment.IsProduction() || stripeIngress.LiveMode;
            var providerIdentityReady = string.Equals(options.AccountId, stripeIngress.AccountId, StringComparison.Ordinal) &&
                                        string.Equals(options.ConnectedAccountId, stripeIngress.ConnectedAccountId, StringComparison.Ordinal) &&
                                        options.LiveMode == stripeIngress.LiveMode;
            var isReady = options.IsEnabled &&
                          (options.UseSimulation
                              ? simulationAllowed
                              : outboundConfigured && webhookConfigured && signatureVerificationEnabled &&
                                providerEnvironmentReady && providerIdentityReady);

            var data = new Dictionary<string, object>
            {
                ["provider"] = "stripe",
                ["enabled"] = options.IsEnabled,
                ["mode"] = options.UseSimulation ? "simulation" : "live",
                ["outboundConfigured"] = outboundConfigured,
                ["webhookConfigured"] = webhookConfigured,
                ["signatureVerificationEnabled"] = signatureVerificationEnabled,
                ["providerEnvironment"] = providerEnvironment,
                ["providerIdentityReady"] = providerIdentityReady
            };

            if (!options.IsEnabled)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Payment provider is disabled.", data));
            }

            return Task.FromResult(isReady
                ? HealthCheckResult.Healthy("Payment provider configuration is ready.", data)
                : HealthCheckResult.Degraded("Payment provider configuration is not ready.", data: data));
        }
        catch
        {
            return Task.FromResult(HealthCheckResult.Degraded("Payment provider readiness check failed."));
        }
    }

    private static bool IsDevelopmentOrTest(string environmentName) =>
        string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
