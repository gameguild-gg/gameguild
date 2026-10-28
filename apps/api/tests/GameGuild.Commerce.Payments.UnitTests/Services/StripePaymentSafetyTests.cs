using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

public sealed class StripePaymentSafetyTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void SettlementVerification_RequiresAnEnabledRealStripeAdapter(bool enabled, bool simulation, bool expected)
    {
        var paymentService = new Mock<IStripePaymentService>();
        var customerService = new Mock<IStripeCustomerService>();
        var gateway = new StripePaymentGateway(
            Options.Create(new StripeGatewayOptions { IsEnabled = enabled, UseSimulation = simulation }),
            paymentService.Object, customerService.Object);

        gateway.SupportsVerifiedSettlement().Should().Be(expected);
        gateway.SupportsVerifiedSettlement("STRIPE").Should().Be(expected);
        gateway.SupportsVerifiedSettlement("paypal").Should().BeFalse();
        paymentService.VerifyNoOtherCalls();
        customerService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("usd", 123.45)]
    [InlineData("jpy", 12345d)]
    public async Task SettlementVerification_SupportedAdapterPreservesTheProviderReceivedAmount(string currency, double expected)
    {
        var options = Options.Create(new StripeGatewayOptions { IsEnabled = true, UseSimulation = false });
        var providerMapper = new StripePaymentService(options, NullLogger<StripePaymentService>.Instance);
        var receipt = providerMapper.MapPaymentIntent(new PaymentIntent
        {
            Id = "pi_verified_settlement",
            Status = "succeeded",
            Currency = currency,
            Amount = 99999,
            AmountReceived = 12345
        });
        var paymentService = new Mock<IStripePaymentService>();
        paymentService.Setup(service => service.GetPaymentAsync("pi_verified_settlement", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);
        var gateway = new StripePaymentGateway(options, paymentService.Object, Mock.Of<IStripeCustomerService>());

        gateway.SupportsVerifiedSettlement("stripe").Should().BeTrue();
        var result = await gateway.GetPaymentAsync("pi_verified_settlement", providerId: "stripe");

        result.SettledAmount.Should().Be((decimal)expected);
        result.SettledCurrency.Should().Be(currency);
        result.Should().BeSameAs(receipt);
        paymentService.Verify(service => service.GetPaymentAsync("pi_verified_settlement", It.IsAny<CancellationToken>()), Times.Once);
        paymentService.VerifyNoOtherCalls();
    }

    [Fact]
    public void IsOutcomeUnknown_ShouldTreatTransportFailureAsAmbiguous()
    {
        StripePaymentService.IsOutcomeUnknown(new StripeException("Connection reset"))
            .Should().BeTrue();
    }

    [Fact]
    public void IsOutcomeUnknown_ShouldTreatProviderFailureAsAmbiguous()
    {
        var exception = new StripeException(
            HttpStatusCode.InternalServerError,
            new StripeError { Type = "api_error" },
            "Provider unavailable");

        StripePaymentService.IsOutcomeUnknown(exception).Should().BeTrue();
    }

    [Fact]
    public void IsOutcomeUnknown_ShouldTreatCardErrorAsDefinitive()
    {
        var exception = new StripeException(
            HttpStatusCode.BadRequest,
            new StripeError { Type = "card_error", Code = "card_declined" },
            "Card declined");

        StripePaymentService.IsOutcomeUnknown(exception).Should().BeFalse();
    }

    [Fact]
    public async Task GetPaymentAsync_ShouldReturnClientActionTokenInSimulation()
    {
        var service = new StripePaymentService(
            Options.Create(new StripeGatewayOptions { UseSimulation = true }),
            NullLogger<StripePaymentService>.Instance);

        var result = await service.GetPaymentAsync("pi_requires_action");

        result.Status.Should().Be(PaymentStatus.RequiresAction);
        result.ClientActionToken.Should().Be("pi_requires_action_secret_simulated");
        result.ProviderMapping.Should().Be(new GatewayProviderMapping(
            "test",
            "acct_simulated",
            "pi_requires_action",
            "payment_intent",
            "capture"));
    }

    [Theory]
    [InlineData("succeeded", "usd", 12345, 123.45)]
    [InlineData("succeeded", "jpy", 12345, 12345d)]
    [InlineData("processing", "usd", 12345, null)]
    [InlineData("succeeded", null, 12345, null)]
    public void MapPaymentIntent_ReportsOnlyTheProviderVerifiedReceivedAmount(
        string status, string? currency, long received, double? expected)
    {
        var service = new StripePaymentService(
            Options.Create(new StripeGatewayOptions { UseSimulation = true }),
            NullLogger<StripePaymentService>.Instance);
        var result = service.MapPaymentIntent(new PaymentIntent
        {
            Id = "pi_saga_settlement",
            Status = status,
            Currency = currency!,
            Amount = 99999,
            AmountReceived = received
        });

        result.SettledAmount.Should().Be(expected.HasValue ? (decimal)expected.Value : null);
        result.SettledCurrency.Should().Be(expected.HasValue ? currency : null);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldReturnCompleteProviderMappingInSimulation()
    {
        var service = new StripePaymentService(
            Options.Create(new StripeGatewayOptions { UseSimulation = true }),
            NullLogger<StripePaymentService>.Instance);

        var result = await service.ProcessPaymentAsync(new GatewayPaymentRequest(
            "payment:test",
            25m,
            "USD",
            null,
            "pm_test",
            "test"));

        result.Success.Should().BeTrue();
        result.ProviderMapping.Should().NotBeNull();
        result.ProviderMapping!.ProviderEnvironment.Should().Be("test");
        result.ProviderMapping.ProviderAccountId.Should().Be("acct_simulated");
        result.ProviderMapping.ProviderObjectId.Should().Be(result.TransactionId);
        result.ProviderMapping.ProviderObjectType.Should().Be("payment_intent");
        result.ProviderMapping.ProviderMonetaryLeg.Should().Be("capture");
    }

    [Fact]
    public void ResolveProviderObjectAccountId_ShouldPreferConnectedAccount()
    {
        var options = new StripeGatewayOptions
        {
            AccountId = "acct_platform",
            ConnectedAccountId = "acct_connected"
        };

        StripePaymentService.ResolveProviderObjectAccountId(options)
            .Should().Be("acct_connected");
    }

    [Fact]
    public void ResolveProviderObjectAccountId_ShouldUsePlatformAccountWithoutConnectContext()
    {
        var options = new StripeGatewayOptions { AccountId = "acct_platform" };

        StripePaymentService.ResolveProviderObjectAccountId(options)
            .Should().Be("acct_platform");
    }

    [Fact]
    public async Task CancelPaymentAsync_ShouldConfirmCancellationInSimulation()
    {
        var service = new StripePaymentService(
            Options.Create(new StripeGatewayOptions { UseSimulation = true }),
            NullLogger<StripePaymentService>.Instance);

        var result = await service.CancelPaymentAsync("pi_failed");

        result.Success.Should().BeTrue();
        result.OutcomeUnknown.Should().BeFalse();
    }
}
