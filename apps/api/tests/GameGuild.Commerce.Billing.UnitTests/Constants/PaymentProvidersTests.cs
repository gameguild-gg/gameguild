using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Commerce.Billing;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Constants;

public class PaymentProvidersTests
{
    [Fact]
    public void IsSupported_Should_Return_True_For_Known_Provider()
    {
        PaymentProviders.IsSupported("Stripe").Should().BeTrue();
    }

    [Fact]
    public void IsSupported_Should_Return_False_For_Unknown_Provider()
    {
        PaymentProviders.IsSupported("unknown").Should().BeFalse();
    }

    [Fact]
    public void Normalize_Should_Lowercase_Provider()
    {
        PaymentProviders.Normalize("STRIPE").Should().Be("stripe");
    }

    [Fact]
    public void CurrencyCodes_IsSupported_Should_Handle_Case()
    {
        CurrencyCodes.IsSupported("usd").Should().BeTrue();
        CurrencyCodes.IsSupported("xyz").Should().BeFalse();
    }

    [Fact]
    public void PayPalSettings_BaseUrl_Should_Use_Live_When_Production()
    {
        var settings = new PayPalSettings { Environment = "live" };

        settings.BaseUrl.Should().Be("https://api-m.paypal.com");
    }

    [Fact]
    public void ApplePaySettings_BaseUrl_Should_Use_Production_When_Set()
    {
        var settings = new ApplePaySettings { Environment = "production" };

        settings.BaseUrl.Should().Be("https://api.storekit.itunes.apple.com");
    }
}