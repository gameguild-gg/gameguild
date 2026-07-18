using FluentAssertions;
using GameGuild.Commerce;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Configuration;

public class BillingConfigurationTests
{
    [Fact]
    public void GetEnabledProviders_Should_Return_Configured_Providers()
    {
        var config = new BillingConfiguration
        {
            Stripe = new StripeSettings { SecretKey = "sk" },
            PayPal = new PayPalSettings { ClientId = "client" },
            ApplePay = new ApplePaySettings { BundleId = "bundle" }
        };

        var providers = config.GetEnabledProviders().ToList();

        providers.Should().Contain(PaymentProviders.Stripe);
        providers.Should().Contain(PaymentProviders.PayPal);
        providers.Should().Contain(PaymentProviders.AppleAppStore);
    }

    [Fact]
    public void IsProviderEnabled_Should_Return_False_For_Unknown()
    {
        var config = new BillingConfiguration();

        config.IsProviderEnabled("unknown").Should().BeFalse();
    }

    [Fact]
    public void GetWebhookSecret_Should_Return_Stripe_And_PayPal_Secrets()
    {
        var config = new BillingConfiguration
        {
            Stripe = new StripeSettings { WebhookSecret = "stripe_secret" },
            PayPal = new PayPalSettings { WebhookId = "paypal_webhook" }
        };

        config.GetWebhookSecret(PaymentProviders.Stripe).Should().Be("stripe_secret");
        config.GetWebhookSecret(PaymentProviders.PayPal).Should().Be("paypal_webhook");
        config.GetWebhookSecret("other").Should().BeNull();
    }

    [Fact]
    public void ValidateProvider_Should_Return_Errors_When_Missing_Config()
    {
        var config = new BillingConfiguration
        {
            Stripe = new StripeSettings { SecretKey = "sk" },
            PayPal = new PayPalSettings { ClientId = "client" },
            ApplePay = new ApplePaySettings { BundleId = "bundle" }
        };    

        var result = config.ValidateProvider();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_Should_Return_Validation_Errors()
    {
        var config = new BillingConfiguration
        {
            Stripe = new StripeSettings { SecretKey = "sk" },
            PayPal = new PayPalSettings { ClientId = "client" },
            ApplePay = new ApplePaySettings { BundleId = "bundle" },
            Webhook = new WebhookSettings { MaxRetryAttempts = -1, ProcessingTimeoutSeconds = 0 }
        };

        var errors = config.Validate(new ValidationContext(config)).ToList();

        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_Should_Reject_Partial_Stripe_Webhook_Configuration()
    {
        var config = new BillingConfiguration
        {
            Stripe = new StripeSettings
            {
                WebhookSecret = "whsec_configured",
                WebhookEndpointId = string.Empty,
                ApiVersion = string.Empty,
                WebhookToleranceSeconds = 0
            }
        };

        var errors = config.Validate(new ValidationContext(config)).ToList();

        errors.Should().Contain(error => error.ErrorMessage!.Contains(nameof(StripeSettings.WebhookEndpointId)));
        errors.Should().Contain(error => error.ErrorMessage!.Contains(nameof(StripeSettings.ApiVersion)));
        errors.Should().Contain(error => error.ErrorMessage!.Contains(nameof(StripeSettings.WebhookToleranceSeconds)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(300)]
    [InlineData(900)]
    public void Validate_Should_Accept_Supported_Stripe_Webhook_Tolerance(long toleranceSeconds)
    {
        var config = new BillingConfiguration
        {
            Stripe = new StripeSettings
            {
                WebhookSecret = "whsec_configured",
                WebhookEndpointId = "we_configured",
                ApiVersion = "2023-10-16",
                WebhookToleranceSeconds = toleranceSeconds
            }
        };

        var errors = config.Validate(new ValidationContext(config)).ToList();

        errors.Should().BeEmpty();
    }
}
