using FluentAssertions;
using GameGuild.Commerce;
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
}