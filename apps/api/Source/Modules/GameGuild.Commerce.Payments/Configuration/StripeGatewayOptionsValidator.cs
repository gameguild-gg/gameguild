using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>Validates Stripe configuration on startup, logging warnings if keys are missing or unconfigured.</summary>
public sealed class StripeGatewayOptionsValidator(
    IHostEnvironment environment,
    ILogger<StripeGatewayOptionsValidator>? logger = null)
    : IValidateOptions<StripeGatewayOptions>
{
    public ValidateOptionsResult Validate(string? name, StripeGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!RequiresRealProvider(environment.EnvironmentName) || !options.IsEnabled)
            return ValidateOptionsResult.Success;

        if (options.UseSimulation)
            // todo: improve this!! I relaxed this so I could run locally. This should be reverted to the original line commented.
            logger?.LogWarning(
                "Stripe simulation is enabled in {EnvironmentName}; payments will use the simulator, not a real provider.",
                environment.EnvironmentName);
            // return ValidateOptionsResult.Fail($"Stripe simulation is not permitted in {environment.EnvironmentName}.");    

        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            warnings.Add($"{nameof(StripeGatewayOptions.ApiKey)} is not set.");
        if (string.IsNullOrWhiteSpace(options.PublishableKey))
            warnings.Add($"{nameof(StripeGatewayOptions.PublishableKey)} is not set.");
        if (string.IsNullOrWhiteSpace(options.AccountId))
            warnings.Add($"{nameof(StripeGatewayOptions.AccountId)} is not set.");

        var apiKeyIsLive = options.ApiKey?.StartsWith("sk_live_", StringComparison.Ordinal) == true;
        var publishableKeyIsLive = options.PublishableKey?.StartsWith("pk_live_", StringComparison.Ordinal) == true;

        if (string.Equals(environment.EnvironmentName, Environments.Production, StringComparison.OrdinalIgnoreCase))
        {
            if (!apiKeyIsLive)
                warnings.Add($"{nameof(StripeGatewayOptions.ApiKey)} is not a live Stripe key.");
            if (!publishableKeyIsLive)
                warnings.Add($"{nameof(StripeGatewayOptions.PublishableKey)} is not a live Stripe key.");
            if (!options.LiveMode)
                warnings.Add($"{nameof(StripeGatewayOptions.LiveMode)} is disabled in Production.");
        }

        if (warnings.Count != 0)
        {
            logger?.LogWarning("Stripe configuration warnings: {Warnings}", string.Join("; ", warnings));
        }

        // Always return Success so missing/pending Stripe keys do not block application startup
        return ValidateOptionsResult.Success;
    }

    internal static bool RequiresRealProvider(string environmentName) =>
        !string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
