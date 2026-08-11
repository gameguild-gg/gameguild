using GameGuild.API.Setup;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using GameGuild.Economy.Risk;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameGuild.API.UnitTests.Core;

public sealed class EconomyProviderCapabilityReadinessTests
{
    [Fact]
    public void DisabledCapability_IsReportedAsDisabledWithoutRequiringStripe()
    {
        var readiness = CreateReadiness();

        readiness.Assess(EconomyValueMovementCapability.PayoutExecution)
            .State.Should().Be(EconomyCapabilityReadinessState.Disabled);
    }

    [Fact]
    public void EnabledNonProviderCapability_IsReadyWithoutStripe()
    {
        var readiness = CreateReadiness(
            enabledCapabilities: [nameof(EconomyValueMovementCapability.ConvertHardToSoft)]);

        readiness.Assess(EconomyValueMovementCapability.ConvertHardToSoft)
            .State.Should().Be(EconomyCapabilityReadinessState.Ready);
    }

    [Fact]
    public void EnabledPayoutCapability_IsNotReadyWhenStripeIsDisabled()
    {
        var readiness = CreateReadiness(
            enabledCapabilities: [nameof(EconomyValueMovementCapability.PayoutExecution)]);

        readiness.Assess(EconomyValueMovementCapability.PayoutExecution)
            .State.Should().Be(EconomyCapabilityReadinessState.ProviderNotReady);
    }

    [Fact]
    public void EnabledPayoutCapability_IsReadyWithReplaySafeStripeConfiguration()
    {
        var readiness = CreateReadiness(
            enabledCapabilities: [nameof(EconomyValueMovementCapability.PayoutExecution)],
            gateway: new StripeGatewayOptions
            {
                IsEnabled = true,
                UseSimulation = false,
                AccountId = "acct_platform",
                LiveMode = false
            },
            billing: CreateBilling());

        readiness.Assess(EconomyValueMovementCapability.PayoutExecution)
            .State.Should().Be(EconomyCapabilityReadinessState.Ready);
    }

    private static EconomyProviderCapabilityReadiness CreateReadiness(
        string[]? enabledCapabilities = null,
        StripeGatewayOptions? gateway = null,
        BillingConfiguration? billing = null) => new(
        Options.Create(new EconomyRiskCompositionOptions
        {
            ValueMovingDecisionsEnabled = enabledCapabilities is not null,
            EnabledCapabilities = enabledCapabilities ?? []
        }),
        Options.Create(gateway ?? new StripeGatewayOptions()),
        Options.Create(billing ?? new BillingConfiguration()),
        new TestHostEnvironment("Staging"));

    private static BillingConfiguration CreateBilling() => new()
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

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GameGuild.API.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
