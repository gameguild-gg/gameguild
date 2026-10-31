using FluentAssertions;
using GameGuild.Commerce.Payments;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     NoOp Plaid adapter tests (issue #414, optional box): while no real Plaid client is
///     configured the port must report itself unavailable and fail every call with the
///     explicit <c>PaymentMethods.PlaidNotConfigured</c> error, so callers deterministically
///     fall back to the micro-deposit verification flow.
/// </summary>
public class NoOpPlaidInstantVerificationTests
{
    [Fact]
    public void Instance_ReportsItselfUnavailable()
    {
        NoOpPlaidInstantVerification.Instance.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task CreateLinkToken_FailsClosedWithTheNotConfiguredError()
    {
        var verification = NoOpPlaidInstantVerification.Instance;

        var result = await verification.CreateLinkTokenAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PaymentMethods.PlaidNotConfigured");
        result.Error.Description.Should().Contain("micro-deposit");
    }

    [Fact]
    public async Task ExchangePublicToken_FailsClosedWithTheNotConfiguredError()
    {
        var verification = new NoOpPlaidInstantVerification();

        var result = await verification.ExchangePublicTokenAsync("public-sandbox-123");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PaymentMethods.PlaidNotConfigured");
    }
}
