using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Configuration;

/// <summary>
///     Validation coverage for the explicit sub-5-second webhook response bound
///     (issue #408) and the Braintree settings block.
/// </summary>
public class WebhookResponseBoundSettingsTests
{
    [Fact]
    public void Default_ResponseBoundSeconds_Should_Be_Strictly_Under_Five()
    {
        var settings = new WebhookSettings();

        settings.ResponseBoundSeconds.Should().BeInRange(1, 4,
            "the response bound must stay strictly under the 5-second provider tolerance");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(30)]
    public void Validate_Should_Reject_ResponseBounds_At_Or_Above_FiveSeconds(int boundSeconds)
    {
        var config = new BillingConfiguration();
        config.Webhook.ResponseBoundSeconds = boundSeconds;

        var results = config.Validate(null!).ToList();

        results.Should().Contain(r => r.ErrorMessage!.Contains("ResponseBoundSeconds"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void Validate_Should_Accept_ResponseBounds_Inside_The_Window(int boundSeconds)
    {
        var config = new BillingConfiguration();
        config.Webhook.ResponseBoundSeconds = boundSeconds;

        var results = config.Validate(new ValidationContext(config)).ToList();

        results.Should().NotContain(r => r.ErrorMessage!.Contains("ResponseBoundSeconds"));
    }

    [Fact]
    public void GetEnabledProviders_Should_Include_Braintree_When_Key_Configured()
    {
        var config = new BillingConfiguration
        {
            Braintree = new BraintreeSettings { PublicKey = "public_key_1234" }
        };

        config.GetEnabledProviders().Should().Contain(PaymentProviders.Braintree);
        config.IsProviderEnabled(PaymentProviders.Braintree).Should().BeTrue();
        config.GetWebhookSecret(PaymentProviders.Braintree).Should().Be("public_key_1234");
    }

    [Fact]
    public void Validate_Should_Reject_Malformed_Braintree_PublicKey()
    {
        var config = new BillingConfiguration
        {
            Braintree = new BraintreeSettings { PublicKey = "short" }
        };

        var results = config.Validate(new ValidationContext(config)).ToList();

        results.Should().Contain(r => r.ErrorMessage!.Contains("Braintree.PublicKey"));
    }

    [Fact]
    public void Validate_Should_Reject_Unknown_Braintree_Environment()
    {
        var config = new BillingConfiguration
        {
            Braintree = new BraintreeSettings { PublicKey = "public_key_1234", Environment = "staging" }
        };

        var results = config.Validate(new ValidationContext(config)).ToList();

        results.Should().Contain(r => r.ErrorMessage!.Contains("Braintree.Environment"));
    }
}
