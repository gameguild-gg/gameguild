using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Configuration;

public sealed class BillingConfigurationProductionValidatorTests
{
    [Fact]
    public void Validate_RejectsMissingStripeWebhookConfigurationInProduction()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, new BillingConfiguration());

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(message => message.Contains(nameof(StripeSettings.WebhookSecret), StringComparison.Ordinal));
        result.Failures.Should().Contain(message => message.Contains(nameof(StripeSettings.WebhookEndpointId), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsNonLiveWebhookInProduction()
    {
        var validator = CreateValidator(Environments.Production);
        var configuration = CreateCompleteConfiguration();
        configuration.Stripe.LiveMode = false;

        var result = validator.Validate(Options.DefaultName, configuration);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(message => message.Contains(nameof(StripeSettings.LiveMode), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AllowsTestModeWebhookInStaging()
    {
        var validator = CreateValidator(Environments.Staging);
        var configuration = CreateCompleteConfiguration();
        configuration.Stripe.LiveMode = false;

        var result = validator.Validate(Options.DefaultName, configuration);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsLiveWebhookInStaging()
    {
        var validator = CreateValidator(Environments.Staging);
        var configuration = CreateCompleteConfiguration();
        configuration.Stripe.LiveMode = true;

        var result = validator.Validate(Options.DefaultName, configuration);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(message => message.Contains("Staging", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Validate_RejectsDisabledWebhookSignatureVerification(string environmentName)
    {
        var validator = CreateValidator(environmentName);
        var configuration = CreateCompleteConfiguration();
        configuration.Webhook.VerifySignatures = false;

        var result = validator.Validate(Options.DefaultName, configuration);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(message => message.Contains(nameof(WebhookSettings.VerifySignatures), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AllowsEmptyStripeConfigurationInTesting()
    {
        var validator = CreateValidator("Testing");

        var result = validator.Validate(Options.DefaultName, new BillingConfiguration());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsCompleteProductionConfiguration()
    {
        var validator = CreateValidator(Environments.Production);
        var configuration = CreateCompleteConfiguration();
        configuration.Stripe.LiveMode = true;

        var result = validator.Validate(Options.DefaultName, configuration);

        result.Succeeded.Should().BeTrue();
    }

    private static BillingConfigurationProductionValidator CreateValidator(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);
        return new BillingConfigurationProductionValidator(environment.Object);
    }

    private static BillingConfiguration CreateCompleteConfiguration() => new()
    {
        Stripe = new StripeSettings
        {
            WebhookSecret = "whsec_test",
            WebhookEndpointId = "we_test",
            AccountId = "acct_platform",
            ApiVersion = "2023-10-16",
            LiveMode = false
        }
    };
}
