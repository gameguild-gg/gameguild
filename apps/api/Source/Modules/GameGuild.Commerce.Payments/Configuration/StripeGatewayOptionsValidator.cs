using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>Prevents value-moving Stripe services from starting with unsafe non-development settings.</summary>
public sealed class StripeGatewayOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<StripeGatewayOptions>
{
    public ValidateOptionsResult Validate(string? name, StripeGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!RequiresRealProvider(environment.EnvironmentName))
            return ValidateOptionsResult.Success;

        var failures = new List<string>();
        if (!options.IsEnabled)
            failures.Add("Stripe must be enabled outside Development and Test environments.");
        if (options.UseSimulation)
            failures.Add("Stripe simulation must be disabled outside Development and Test environments.");
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            failures.Add($"{nameof(StripeGatewayOptions.ApiKey)} is required when Stripe is enabled.");
        if (string.IsNullOrWhiteSpace(options.PublishableKey))
            failures.Add($"{nameof(StripeGatewayOptions.PublishableKey)} is required when Stripe is enabled.");
        if (string.Equals(environment.EnvironmentName, Environments.Production, StringComparison.OrdinalIgnoreCase))
        {
            if (options.ApiKey?.StartsWith("sk_live_", StringComparison.Ordinal) != true)
                failures.Add($"{nameof(StripeGatewayOptions.ApiKey)} must be a live Stripe key in Production.");
            if (options.PublishableKey?.StartsWith("pk_live_", StringComparison.Ordinal) != true)
                failures.Add($"{nameof(StripeGatewayOptions.PublishableKey)} must be a live Stripe key in Production.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    internal static bool RequiresRealProvider(string environmentName) =>
        !string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
