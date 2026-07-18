using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Billing;

/// <summary>Requires an authentic Stripe webhook/provider configuration outside local and test environments.</summary>
public sealed class BillingConfigurationProductionValidator(IHostEnvironment environment)
    : IValidateOptions<BillingConfiguration>
{
    public ValidateOptionsResult Validate(string? name, BillingConfiguration options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!RequiresProviderConfiguration(environment.EnvironmentName))
            return ValidateOptionsResult.Success;

        var failures = new List<string>();
        var stripe = options.Stripe;
        if (string.IsNullOrWhiteSpace(stripe.WebhookSecret))
            failures.Add($"Stripe.{nameof(StripeSettings.WebhookSecret)} is required outside Development and Test environments.");
        if (string.IsNullOrWhiteSpace(stripe.WebhookEndpointId))
            failures.Add($"Stripe.{nameof(StripeSettings.WebhookEndpointId)} is required outside Development and Test environments.");
        if (string.IsNullOrWhiteSpace(stripe.AccountId))
            failures.Add($"Stripe.{nameof(StripeSettings.AccountId)} is required outside Development and Test environments.");
        if (string.IsNullOrWhiteSpace(stripe.ApiVersion))
            failures.Add($"Stripe.{nameof(StripeSettings.ApiVersion)} is required outside Development and Test environments.");
        if (!options.Webhook.VerifySignatures)
            failures.Add($"Webhook.{nameof(WebhookSettings.VerifySignatures)} must be true outside Development and Test environments.");
        if (string.Equals(environment.EnvironmentName, Environments.Production, StringComparison.OrdinalIgnoreCase) && !stripe.LiveMode)
            failures.Add($"Stripe.{nameof(StripeSettings.LiveMode)} must be true in Production.");
        if (string.Equals(environment.EnvironmentName, Environments.Staging, StringComparison.OrdinalIgnoreCase) && stripe.LiveMode)
            failures.Add($"Stripe.{nameof(StripeSettings.LiveMode)} must be false in Staging.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool RequiresProviderConfiguration(string environmentName) =>
        !string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
