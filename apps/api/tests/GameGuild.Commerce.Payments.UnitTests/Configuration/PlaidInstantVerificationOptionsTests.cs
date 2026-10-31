using FluentAssertions;
using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Configuration;

/// <summary>
///     Plaid instant-verification configuration tests (issue #414, optional box): the
///     options fail closed (disabled without credentials) and the validator rejects an
///     enabled-but-uncredentialed or malformed configuration at startup.
/// </summary>
public class PlaidInstantVerificationOptionsTests
{
    [Fact]
    public void Defaults_AreFailClosedToTheSandbox()
    {
        var options = new PlaidInstantVerificationOptions();

        options.IsEnabled.Should().BeFalse();
        options.Environment.Should().Be("sandbox");
        options.ClientId.Should().BeEmpty();
        options.ClientSecret.Should().BeEmpty();
        options.Products.Should().BeEquivalentTo(["auth"]);
        PlaidInstantVerificationOptions.SectionName.Should().Be("Payments:Plaid");
    }

    [Fact]
    public void Validator_AcceptsTheDisabledDefaultWithoutCredentials()
    {
        var validator = new PlaidInstantVerificationOptionsValidator();
        var result = validator.Validate(null, new PlaidInstantVerificationOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validator_AcceptsAnEnabledConfigurationWithCredentials()
    {
        var validator = new PlaidInstantVerificationOptionsValidator();
        var result = validator.Validate(null, new PlaidInstantVerificationOptions
        {
            IsEnabled = true,
            ClientId = "plaid-client-id",
            ClientSecret = "plaid-secret"
        });

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, "", "secret", nameof(PlaidInstantVerificationOptions.ClientId))]
    [InlineData(true, "id", "", nameof(PlaidInstantVerificationOptions.ClientSecret))]
    [InlineData(false, "", "", "Environment")]
    public void Validator_RejectsFailOpenConfigurations(bool enabled, string clientId, string clientSecret, string expectedFailureFragment)
    {
        var options = new PlaidInstantVerificationOptions
        {
            IsEnabled = enabled,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Environment = "live"
        };

        var validator = new PlaidInstantVerificationOptionsValidator();
        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(expectedFailureFragment);
    }

    [Fact]
    public void Validator_RejectsAnEmptyProductsList()
    {
        var validator = new PlaidInstantVerificationOptionsValidator();
        var result = validator.Validate(null, new PlaidInstantVerificationOptions { Products = [] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(PlaidInstantVerificationOptions.Products));
    }

    [Theory]
    [InlineData("ftp://sandbox.plaid.com")]
    [InlineData("not-a-url")]
    public void Validator_RejectsInvalidBaseUrls(string apiBaseUrl)
    {
        var validator = new PlaidInstantVerificationOptionsValidator();
        var result = validator.Validate(null, new PlaidInstantVerificationOptions { ApiBaseUrl = apiBaseUrl });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(PlaidInstantVerificationOptions.ApiBaseUrl));
    }

    [Theory]
    [InlineData("sandbox", "https://sandbox.plaid.com")]
    [InlineData("production", "https://production.plaid.com")]
    [InlineData("unknown", "https://sandbox.plaid.com", "unknown environments fail closed to the sandbox")]
    public void ResolveBaseUrl_DerivesTheEnvironmentEndpoint(string environment, string expectedBaseUrl, string? because = null)
    {
        var options = new PlaidInstantVerificationOptions { Environment = environment };

        options.ResolveBaseUrl().Should().Be(expectedBaseUrl, because);
        options.EnvironmentLabel.Should().Be(environment == "production" ? "production" : "sandbox");
    }

    [Fact]
    public void ResolveBaseUrl_ExplicitOverrideWins()
    {
        var options = new PlaidInstantVerificationOptions { Environment = "production", ApiBaseUrl = "https://plaid.proxy.local/" };

        options.ResolveBaseUrl().Should().Be("https://plaid.proxy.local");
    }
}
