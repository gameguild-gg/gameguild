using FluentAssertions;
using GameGuild.API.Setup;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using GameGuild.Compliance.FinancialCrime;
using GameGuild.Economy.Risk;
using GameGuild.TrustSafety;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GameGuild.API.UnitTests.Core;

public sealed class EconomyCapabilityCompositionTests
{
    [Fact]
    public async Task ApiComposesFailClosedFinancialRiskInputsWithoutEnablingCapabilities()
    {
        var services = new ServiceCollection();
        services.AddEconomyCapabilityComposition(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        var gate = provider.GetRequiredService<IEconomyValueMovementDecisionGate>();
        var financialCrime = provider.GetRequiredService<IFinancialCrimeRiskInputSource>();
        var trustSafety = provider.GetRequiredService<ITrustSafetyRiskInputSource>();
        var now = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

        gate.IsEnabled.Should().BeFalse();
        (await financialCrime.ReadAsync("actor-token", now)).Outcome
            .Should().Be(ExternalRiskOutcome.Unavailable);
        (await trustSafety.ReadAsync("actor-token", now)).Outcome
            .Should().Be(ExternalRiskOutcome.Unavailable);
    }

    [Fact]
    public void ProviderBackedCapabilityRequiresVerifiedReplaySafeStripeIngress()
    {
        var economy = new EconomyRiskCompositionOptions
        {
            ValueMovingDecisionsEnabled = true,
            EnabledCapabilities = [nameof(EconomyValueMovementCapability.ConfirmHardCoinFunding)]
        };
        var gateway = new StripeGatewayOptions
        {
            IsEnabled = true,
            UseSimulation = false,
            AccountId = "acct_platform",
            LiveMode = false
        };
        var billing = new BillingConfiguration
        {
            Stripe = new StripeSettings
            {
                AccountId = "acct_platform",
                LiveMode = false,
                WebhookSecret = "whsec_economy",
                WebhookEndpointId = "we_economy",
                ApiVersion = "2025-01-27.acacia",
                WebhookToleranceSeconds = 300
            },
            Webhook = new WebhookSettings
            {
                VerifySignatures = true,
                StorePayloads = true
            }
        };

        FluentActions.Invoking(() => EconomyProviderCapabilityGuard.ThrowIfInvalid(
                economy, gateway, billing, "Staging"))
            .Should().NotThrow();

        billing.Stripe.WebhookSecret = string.Empty;
        FluentActions.Invoking(() => EconomyProviderCapabilityGuard.ThrowIfInvalid(
                economy, gateway, billing, "Staging"))
            .Should().Throw<EconomyProviderConfigurationException>()
            .WithMessage("*WebhookSecret*");
    }

    [Fact]
    public void NonProviderCapabilityDoesNotRequireStripeIngress()
    {
        var economy = new EconomyRiskCompositionOptions
        {
            ValueMovingDecisionsEnabled = true,
            EnabledCapabilities = [nameof(EconomyValueMovementCapability.Transfer)]
        };

        FluentActions.Invoking(() => EconomyProviderCapabilityGuard.ThrowIfInvalid(
                economy, new StripeGatewayOptions(), new BillingConfiguration(), "Production"))
            .Should().NotThrow();
    }

    [Fact]
    public void ProviderReadinessIsComposedWithoutMakingStripeAStartupRequirement()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Staging"));
        services.AddEconomyCapabilityComposition(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        var readiness = provider.GetRequiredService<IEconomyProviderCapabilityReadiness>();

        readiness.Assess(EconomyValueMovementCapability.PayoutExecution)
            .State.Should().Be(EconomyCapabilityReadinessState.Disabled);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GameGuild.API.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
