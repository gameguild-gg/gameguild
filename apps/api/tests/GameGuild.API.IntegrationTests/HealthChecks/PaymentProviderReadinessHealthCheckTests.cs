using FluentAssertions;
using GameGuild.API.HealthChecks;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests.HealthChecks;

public sealed class PaymentProviderReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDevelopmentUsesSimulation_ReturnsHealthyWithoutExposingKeys()
    {
        var options = new StripeGatewayOptions
        {
            IsEnabled = true,
            UseSimulation = true,
            ApiKey = "sk_test_sensitive-api-key",
            PublishableKey = "pk_test_sensitive-publishable-key",
            AccountId = "acct_platform",
            LiveMode = false
        };
        var healthCheck = CreateHealthCheck(options, Environments.Development);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["provider"] = "stripe",
            ["enabled"] = true,
            ["mode"] = "simulation",
            ["outboundConfigured"] = true,
            ["webhookConfigured"] = true,
            ["signatureVerificationEnabled"] = true,
            ["providerEnvironment"] = "test",
            ["providerIdentityReady"] = true
        });
        AssertSanitized(
            result,
            options.ApiKey,
            options.PublishableKey,
            "whsec_sensitive-webhook",
            "we_sensitive-endpoint",
            "2026-06-01");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenProductionUsesLiveProvider_ReturnsHealthyWithoutExposingKeys()
    {
        var options = new StripeGatewayOptions
        {
            IsEnabled = true,
            UseSimulation = false,
            ApiKey = "sk_live_sensitive-api-key",
            PublishableKey = "pk_live_sensitive-publishable-key",
            AccountId = "acct_platform",
            LiveMode = true
        };
        var healthCheck = CreateHealthCheck(options, Environments.Production);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["provider"] = "stripe",
            ["enabled"] = true,
            ["mode"] = "live",
            ["outboundConfigured"] = true,
            ["webhookConfigured"] = true,
            ["signatureVerificationEnabled"] = true,
            ["providerEnvironment"] = "live",
            ["providerIdentityReady"] = true
        });
        AssertSanitized(
            result,
            options.ApiKey,
            options.PublishableKey,
            "whsec_sensitive-webhook",
            "we_sensitive-endpoint",
            "2026-06-01");
    }

    [Theory]
    [InlineData(false, false, "sk_live_sensitive-api-key", "pk_live_sensitive-publishable-key")]
    [InlineData(true, true, "sk_live_sensitive-api-key", "pk_live_sensitive-publishable-key")]
    [InlineData(true, false, "", "pk_live_sensitive-publishable-key")]
    [InlineData(true, false, "sk_live_sensitive-api-key", "")]
    [InlineData(true, false, "sk_test_sensitive-api-key", "pk_test_sensitive-publishable-key")]
    public async Task CheckHealthAsync_WhenProductionProviderIsUnsafe_ReturnsSanitizedUnhealthyResult(
        bool isEnabled,
        bool useSimulation,
        string apiKey,
        string publishableKey)
    {
        var options = new StripeGatewayOptions
        {
            IsEnabled = isEnabled,
            UseSimulation = useSimulation,
            ApiKey = apiKey,
            PublishableKey = publishableKey,
            AccountId = "acct_platform",
            LiveMode = true
        };
        var healthCheck = CreateHealthCheck(options, Environments.Production);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Payment provider configuration is not ready.");
        result.Data.Keys.Should().BeEquivalentTo(
            "provider",
            "enabled",
            "mode",
            "outboundConfigured",
            "webhookConfigured",
            "signatureVerificationEnabled",
            "providerEnvironment",
            "providerIdentityReady");
        AssertSanitized(result, apiKey, publishableKey);
    }

    [Theory]
    [InlineData("", "we_sensitive-endpoint", "2026-06-01", true, true)]
    [InlineData("whsec_sensitive-webhook", "", "2026-06-01", true, true)]
    [InlineData("whsec_sensitive-webhook", "we_sensitive-endpoint", "", true, true)]
    [InlineData("whsec_sensitive-webhook", "we_sensitive-endpoint", "2026-06-01", false, true)]
    [InlineData("whsec_sensitive-webhook", "we_sensitive-endpoint", "2026-06-01", true, false)]
    public async Task CheckHealthAsync_WhenProductionIngressIsUnsafe_ReturnsSanitizedUnhealthyResult(
        string webhookSecret,
        string webhookEndpointId,
        string apiVersion,
        bool verifySignatures,
        bool liveMode)
    {
        var outboundOptions = new StripeGatewayOptions
        {
            IsEnabled = true,
            UseSimulation = false,
            ApiKey = "sk_live_sensitive-api-key",
            PublishableKey = "pk_live_sensitive-publishable-key",
            AccountId = "acct_platform",
            LiveMode = true
        };
        var billingConfiguration = CreateBillingConfiguration(liveMode);
        billingConfiguration.Stripe.WebhookSecret = webhookSecret;
        billingConfiguration.Stripe.WebhookEndpointId = webhookEndpointId;
        billingConfiguration.Stripe.ApiVersion = apiVersion;
        billingConfiguration.Webhook.VerifySignatures = verifySignatures;
        var healthCheck = CreateHealthCheck(outboundOptions, Environments.Production, billingConfiguration);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Payment provider configuration is not ready.");
        result.Data.Should().Contain("webhookConfigured", !string.IsNullOrWhiteSpace(webhookSecret) &&
                                                         !string.IsNullOrWhiteSpace(webhookEndpointId) &&
                                                         !string.IsNullOrWhiteSpace(apiVersion));
        result.Data.Should().Contain("signatureVerificationEnabled", verifySignatures);
        result.Data.Should().Contain("providerEnvironment", liveMode ? "live" : "test");
        AssertSanitized(
            result,
            outboundOptions.ApiKey,
            outboundOptions.PublishableKey,
            webhookSecret,
            webhookEndpointId,
            apiVersion);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOptionsAccessFails_ReturnsSanitizedUnhealthyResult()
    {
        const string sensitiveFailure = "options failure containing sk_live_sensitive-api-key";
        var healthCheck = new PaymentProviderReadinessHealthCheck(
            new ThrowingOptions<StripeGatewayOptions>(sensitiveFailure),
            Options.Create(CreateBillingConfiguration(liveMode: true)),
            new TestHostEnvironment(Environments.Production));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Payment provider readiness check failed.");
        AssertSanitized(result, sensitiveFailure);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBillingOptionsAccessFails_ReturnsSanitizedUnhealthyResult()
    {
        const string sensitiveFailure = "billing options failure containing whsec_sensitive-webhook";
        var healthCheck = new PaymentProviderReadinessHealthCheck(
            Options.Create(new StripeGatewayOptions
            {
                IsEnabled = true,
                UseSimulation = false,
                ApiKey = "sk_live_sensitive-api-key",
                PublishableKey = "pk_live_sensitive-publishable-key",
                AccountId = "acct_platform",
                LiveMode = true
            }),
            new ThrowingOptions<BillingConfiguration>(sensitiveFailure),
            new TestHostEnvironment(Environments.Production));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Payment provider readiness check failed.");
        AssertSanitized(result, sensitiveFailure);
    }

    private static PaymentProviderReadinessHealthCheck CreateHealthCheck(
        StripeGatewayOptions options,
        string environmentName,
        BillingConfiguration? billingConfiguration = null) =>
        new(
            Options.Create(options),
            Options.Create(billingConfiguration ??
                           CreateBillingConfiguration(string.Equals(
                               environmentName,
                               Environments.Production,
                               StringComparison.OrdinalIgnoreCase))),
            new TestHostEnvironment(environmentName));

    private static BillingConfiguration CreateBillingConfiguration(bool liveMode) => new()
    {
        Stripe = new StripeSettings
        {
            WebhookSecret = "whsec_sensitive-webhook",
            WebhookEndpointId = "we_sensitive-endpoint",
            AccountId = "acct_platform",
            ApiVersion = "2026-06-01",
            LiveMode = liveMode
        },
        Webhook = new WebhookSettings
        {
            VerifySignatures = true
        }
    };

    private static void AssertSanitized(HealthCheckResult result, params string[] sensitiveValues)
    {
        result.Exception.Should().BeNull();

        var exposedOutput = string.Join(
            "|",
            result.Data.SelectMany(entry => new[] { entry.Key, entry.Value?.ToString() ?? string.Empty })
                .Prepend(result.Description ?? string.Empty));

        foreach (var sensitiveValue in sensitiveValues.Where(value => !string.IsNullOrEmpty(value)))
        {
            exposedOutput.Should().NotContain(sensitiveValue);
        }
    }

    private sealed class ThrowingOptions<T>(string message) : IOptions<T>
        where T : class
    {
        public T Value => throw new InvalidOperationException(message);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "GameGuild.API.IntegrationTests";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
