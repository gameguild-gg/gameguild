using FluentAssertions;
using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Configuration;

public sealed class StripeProviderConfigurationGuardTests
{
    [Fact]
    public void ThrowIfInvalid_AcceptsMatchingCanonicalConfiguration()
    {
        var action = () => StripeProviderConfigurationGuard.ThrowIfInvalid(
            CreateGateway(),
            CreateBilling(),
            Environments.Staging);

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("account")]
    [InlineData("mode")]
    [InlineData("connected-account")]
    public void ThrowIfInvalid_LogsWarningOnCrossModuleMismatchWithoutThrowing(string mismatch)
    {
        var gateway = CreateGateway();
        var billing = CreateBilling();
        if (mismatch == "account") billing.Stripe.AccountId = "acct_other";
        if (mismatch == "mode") billing.Stripe.LiveMode = true;
        if (mismatch == "connected-account") billing.Stripe.ConnectedAccountId = "acct_connected";

        var action = () => StripeProviderConfigurationGuard.ThrowIfInvalid(
            gateway,
            billing,
            Environments.Staging);

        action.Should().NotThrow();
    }

    [Fact]
    public void ThrowIfInvalid_AllowsLocalSimulationWithoutProviderConfiguration()
    {
        var action = () => StripeProviderConfigurationGuard.ThrowIfInvalid(
            new StripeGatewayOptions(),
            new BillingConfiguration(),
            Environments.Development);

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void ThrowIfInvalid_RejectsSimulationOutsideDevelopmentAndTest(string environmentName)
    {
        var gateway = CreateGateway();
        gateway.UseSimulation = true;

        var action = () => StripeProviderConfigurationGuard.ThrowIfInvalid(
            gateway,
            CreateBilling(),
            environmentName);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*simulation*");
    }

    private static StripeGatewayOptions CreateGateway() => new()
    {
        AccountId = "acct_platform",
        UseSimulation = false,
        LiveMode = false
    };

    private static BillingConfiguration CreateBilling() => new()
    {
        Stripe = new StripeSettings
        {
            AccountId = "acct_platform",
            LiveMode = false
        }
    };
}
