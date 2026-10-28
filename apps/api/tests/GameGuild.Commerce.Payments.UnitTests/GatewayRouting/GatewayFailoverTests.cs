using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.GatewayRouting;

/// <summary>
///     Failover sequence tests for <see cref="FailoverPaymentGateway" /> (issue #413):
///     same-gateway retry on retriable failures then next-priority failover with
///     skip-attempted and the max-3-attempt budget; terminal declines and outcome-unknown
///     results stop the loop; the all-gateways-down scenario alerts and fails closed.
/// </summary>
public class GatewayFailoverTests
{
    private readonly RecordingGatewayAvailabilityAlerter _alerter = new();

    private readonly GatewayFailoverOptions _failoverOptions = new()
    {
        MaxAttemptsTotal = 3,
        SameGatewayRetryLimit = 1,
        CircuitFailureThreshold = 5,
        CircuitOpenSeconds = 60
    };

    private readonly PaymentRetryOptions _paymentRetryOptions = new() { MaxRetries = 3 };

    private FailoverPaymentGateway CreateSut(
        IReadOnlyList<IPaymentGateway> gateways,
        InMemoryGatewayCircuitBreaker? circuitBreaker = null,
        GatewayRoutingOptions? routing = null,
        PaymentRetryOptions? paymentRetry = null)
    {
        var registry = new FakeGatewayRegistry([.. gateways]);
        var selector = new ConfigPaymentGatewaySelector(
            registry,
            Options.Create(routing ?? new GatewayRoutingOptions()),
            NullLogger<ConfigPaymentGatewaySelector>.Instance);

        return new FailoverPaymentGateway(
            registry,
            selector,
            circuitBreaker ?? new InMemoryGatewayCircuitBreaker(_failoverOptions),
            _alerter,
            Options.Create(_failoverOptions),
            Options.Create(paymentRetry ?? _paymentRetryOptions),
            NullLogger<FailoverPaymentGateway>.Instance);
    }

    private static GatewayPaymentRequest ChargeRequest(decimal amount = 25m, string currency = "USD") =>
        new("idem-1", amount, currency, "cus_1", "pm_card_visa", "test charge");

    /// <summary>
    ///     Composed retry budget (issue #419): a refund execution carrying
    ///     <see cref="GatewayRefundRequest.MaxProviderAttempts"/> clamps the composite's
    ///     same-gateway retry loop so the provider calls across the whole workflow never
    ///     exceed the workflow maximum.
    /// </summary>
    [Fact]
    public async Task Refund_CallerProvidedBudget_ClampsTheCompositeRetryLoop()
    {
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueRefund(FakePaymentGateway.RefundFailureResult(GatewayFailureKind.RetriableGatewayFailure))
            .EnqueueRefund(FakePaymentGateway.RefundFailureResult(GatewayFailureKind.RetriableGatewayFailure));
        var sut = CreateSut([stripe]);

        var result = await sut.ProcessRefundAsync(new GatewayRefundRequest(
            "idem-refund-1",
            "ch_1",
            10m,
            "goodwill",
            ProviderId: "stripe",
            MaxProviderAttempts: 1));

        // The configured budget would retry once (SameGatewayRetryLimit=1 => 2 attempts); the
        // caller budget of 1 allows exactly one provider call.
        result.Success.Should().BeFalse();
        stripe.RefundCalls.Should().ContainSingle();
    }

    [Fact]
    public async Task Refund_WithoutCallerBudget_UsesTheConfiguredCompositeBudget()
    {
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueRefund(FakePaymentGateway.RefundFailureResult(GatewayFailureKind.RetriableGatewayFailure))
            .EnqueueRefund(FakePaymentGateway.RefundFailureResult(GatewayFailureKind.RetriableGatewayFailure));
        var sut = CreateSut([stripe]);

        var result = await sut.ProcessRefundAsync(new GatewayRefundRequest(
            "idem-refund-2",
            "ch_1",
            10m,
            "goodwill",
            ProviderId: "stripe"));

        result.Success.Should().BeFalse();
        stripe.RefundCalls.Should().HaveCount(2, "the configured SameGatewayRetryLimit=1 budget allows two attempts");
    }

    [Fact]
    public void SettlementVerification_IsPinnedToTheRequestedProviderAndDefaultsToUnsupported()
    {
        var stripe = new Mock<IPaymentGateway>();
        stripe.SetupGet(gateway => gateway.ProviderId).Returns("stripe");
        stripe.SetupGet(gateway => gateway.IsEnabled).Returns(true);
        stripe.Setup(gateway => gateway.SupportsVerifiedSettlement("stripe")).Returns(true);
        var paypal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe.Object, paypal]);

        sut.SupportsVerifiedSettlement("paypal").Should().BeFalse();
        sut.SupportsVerifiedSettlement("stripe").Should().BeTrue();

        paypal.ChargeCallCount.Should().Be(0);
        stripe.Verify(gateway => gateway.SupportsVerifiedSettlement("stripe"), Times.Once);
        stripe.Verify(gateway => gateway.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SettlementVerification_UnknownProviderNeverFallsBackToPrimary()
    {
        var stripe = new FakePaymentGateway("stripe");
        var sut = CreateSut([stripe]);

        Action act = () => sut.SupportsVerifiedSettlement("unknown-gateway");

        act.Should().Throw<GatewaySelectionFailedException>();
        stripe.ChargeCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Charge_ProviderBoundToken_IsNeverSentToAnotherGateway()
    {
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.RetriableGatewayFailure))
            .EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.RetriableGatewayFailure));
        var paypal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, paypal]);

        var result = await sut.ProcessPaymentAsync(ChargeRequest() with { GatewayProvider = "stripe" });

        result.Success.Should().BeFalse();
        stripe.ChargeCallCount.Should().Be(2);
        paypal.ChargeCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Charge_UnavailableTokenProvider_FailsBeforeAnyGatewayCall()
    {
        var paypal = new FakePaymentGateway("paypal");
        var sut = CreateSut([paypal]);

        var act = () => sut.ProcessPaymentAsync(ChargeRequest() with { GatewayProvider = "stripe" });

        await act.Should().ThrowAsync<GatewaySelectionFailedException>();
        paypal.ChargeCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Charge_ReturnsFirstGatewaySuccess_WithAttemptedProviderAttribution()
    {
        var stripe = new FakePaymentGateway("stripe");
        var sut = CreateSut([stripe, new FakePaymentGateway("paypal")]);

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeTrue();
        result.ProviderId.Should().Be("stripe");
        stripe.ChargeCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Charge_RetriesSameGatewayOnce_ThenFailsOverToNextPriority()
    {
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.RetriableGatewayFailure))
            .EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.RetriableGatewayFailure));
        var payPal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, payPal]);

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeTrue();
        result.ProviderId.Should().Be("paypal");
        stripe.ChargeCallCount.Should().Be(2, "one attempt plus one same-gateway retry");
        payPal.ChargeCallCount.Should().Be(1, "failover reaches the next-priority gateway once");
    }

    [Fact]
    public async Task Charge_StopsAtMaxThreeAttempts_TotalAcrossGateways()
    {
        var stripe = new FakePaymentGateway("stripe");
        for (var i = 0; i < 5; i++)
        {
            stripe.EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.RetriableGatewayFailure));
        }

        var payPal = new FakePaymentGateway("paypal")
            .EnqueueCharge(FakePaymentGateway.FailureResult("paypal", GatewayFailureKind.RetriableGatewayFailure));

        var sut = CreateSut([stripe, payPal]);

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeFalse();
        result.ProviderId.Should().Be("paypal");
        result.FailureKind.Should().Be(GatewayFailureKind.RetriableGatewayFailure);
        stripe.ChargeCallCount.Should().Be(2);
        payPal.ChargeCallCount.Should().Be(1);
        (stripe.ChargeCallCount + payPal.ChargeCallCount).Should().Be(3, "the attempt budget is three");
    }

    [Fact]
    public async Task Charge_DoesNotRetryOrFailOver_OnTerminalDecline()
    {
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.TerminalDecline, "card_declined"));
        var payPal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, payPal]);

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("card_declined");
        result.ProviderId.Should().Be("stripe");
        stripe.ChargeCallCount.Should().Be(1);
        payPal.ChargeCallCount.Should().Be(0, "a decline is never replayed on another gateway");
    }

    [Fact]
    public async Task Charge_DoesNotRetryOrFailOver_OnOutcomeUnknown()
    {
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.OutcomeUnknown, "stripe_outcome_unknown"));
        var payPal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, payPal]);

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeFalse();
        result.FailureKind.Should().Be(GatewayFailureKind.OutcomeUnknown);
        stripe.ChargeCallCount.Should().Be(1);
        payPal.ChargeCallCount.Should().Be(0, "an unknown outcome is left to reconciliation, never replayed");
    }

    [Fact]
    public async Task Charge_SkipsSameGatewayRetry_WhenPaymentRetryBudgetIsZero()
    {
        // Payments:Retry:MaxRetries = 0 disables the same-gateway retry; failover moves
        // straight to the next-priority gateway.
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueCharge(FakePaymentGateway.FailureResult("stripe", GatewayFailureKind.RetriableGatewayFailure));
        var payPal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, payPal], paymentRetry: new PaymentRetryOptions { MaxRetries = 0 });

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeTrue();
        result.ProviderId.Should().Be("paypal");
        stripe.ChargeCallCount.Should().Be(1);
        payPal.ChargeCallCount.Should().Be(1);
    }

    public static IEnumerable<object?[]> AmbiguousProviderFailures()
    {
        foreach (var provider in new[] { "paypal", "adyen", "braintree", "authorizenet" })
        foreach (var status in new int?[] { null, 408, 429, 500, 503 })
            yield return [provider, status];
    }

    [Theory]
    [MemberData(nameof(AmbiguousProviderFailures))]
    public async Task Charge_RealAdapterWithLostOrAmbiguousResponse_DoesNotReplay(string provider, int? status)
    {
        var apiCalls = 0;
        var failure = new GatewayProviderApiResult(false, status, null, "response_unconfirmed", "Response lost after send", status is null);
        IPaymentGateway adapter;
        switch (provider)
        {
            case "paypal":
                var paypal = new Mock<IPayPalApiClient>();
                paypal.Setup(client => client.CreateAndCaptureOrderAsync(It.IsAny<string>(), It.IsAny<decimal>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .Callback(() => apiCalls++)
                    .ReturnsAsync(new PayPalApiResult(false, status, null, failure.ErrorCode, failure.ErrorMessage, status is null));
                adapter = new PayPalPaymentGateway(Options.Create(new PayPalGatewayOptions { IsEnabled = true }),
                    paypal.Object, NullLogger<PayPalPaymentGateway>.Instance);
                break;
            case "adyen":
                var adyen = new Mock<IAdyenApiClient>();
                adyen.Setup(client => client.PaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback(() => apiCalls++).ReturnsAsync(failure);
                adapter = new AdyenPaymentGateway(Options.Create(new AdyenGatewayOptions { IsEnabled = true }), adyen.Object);
                break;
            case "braintree":
                var braintree = new Mock<IBraintreeApiClient>();
                braintree.Setup(client => client.SaleAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                        It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .Callback(() => apiCalls++).ReturnsAsync(failure);
                adapter = new BraintreePaymentGateway(Options.Create(new BraintreeGatewayOptions { IsEnabled = true }), braintree.Object);
                break;
            default:
                var authorizenet = new Mock<IAuthorizeNetApiClient>();
                authorizenet.Setup(client => client.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .Callback(() => apiCalls++).ReturnsAsync(failure);
                adapter = new AuthorizeNetPaymentGateway(Options.Create(new AuthorizeNetGatewayOptions { IsEnabled = true }), authorizenet.Object);
                break;
        }
        var fallback = new FakePaymentGateway("fallback");
        var routing = new GatewayRoutingOptions
        {
            Gateways = [new GatewayRouteEntry { ProviderId = provider, Priority = 1 },
                new GatewayRouteEntry { ProviderId = "fallback", Priority = 2 }]
        };

        var result = await CreateSut([adapter, fallback], routing: routing).ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeFalse();
        result.Status.Should().Be(PaymentStatus.Processing);
        result.FailureKind.Should().Be(GatewayFailureKind.OutcomeUnknown);
        result.ProviderId.Should().Be(provider);
        apiCalls.Should().Be(1, "neither the same provider nor another provider may repeat an unconfirmed charge");
        fallback.ChargeCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("http", false)]
    [InlineData("timeout", false)]
    [InlineData("canceled", false)]
    [InlineData("http", true)]
    [InlineData("timeout", true)]
    [InlineData("canceled", true)]
    public async Task Charge_TransportExceptionAfterAttempt_DoesNotRetryOrFailOver(string failure, bool pinned)
    {
        Exception exception = failure switch
        {
            "timeout" => new TimeoutException("Response lost after send"),
            "canceled" => new TaskCanceledException("Response lost after send"),
            _ => new HttpRequestException("Response lost after send")
        };
        var stripe = new FakePaymentGateway("stripe")
            .EnqueueCharge(_ => throw exception);
        var payPal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, payPal]);

        var result = await sut.ProcessPaymentAsync(ChargeRequest() with { GatewayProvider = pinned ? "stripe" : null });

        result.Success.Should().BeFalse();
        result.Status.Should().Be(PaymentStatus.Processing);
        result.FailureKind.Should().Be(GatewayFailureKind.OutcomeUnknown);
        result.ProviderId.Should().Be("stripe");
        stripe.ChargeCallCount.Should().Be(1);
        payPal.ChargeCallCount.Should().Be(0, "a lost response may hide a successful charge at the first provider");
    }

    [Fact]
    public async Task Charge_FailsClosedWithAlert_WhenEveryCircuitIsOpen()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var circuitBreaker = new InMemoryGatewayCircuitBreaker(new GatewayFailoverOptions
        {
            CircuitFailureThreshold = 1,
            CircuitOpenSeconds = 60
        });

        // Open both circuits by recording one retriable failure each (threshold 1).
        circuitBreaker.RecordFailure("stripe");
        circuitBreaker.RecordFailure("paypal");

        var sut = CreateSut([stripe, payPal], circuitBreaker: circuitBreaker);

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(FailoverPaymentGateway.AllGatewaysUnavailableErrorCode);
        result.Status.Should().Be(PaymentStatus.Failed);
        stripe.ChargeCallCount.Should().Be(0);
        payPal.ChargeCallCount.Should().Be(0);
        _alerter.RaisedCircuitStates.Should().HaveCount(1);
        _alerter.RaisedCircuitStates[0].Select(state => state.ProviderId).Should().Contain("stripe", "paypal");
    }

    [Fact]
    public async Task Charge_SkipsGatewayWithOpenCircuit_AndUsesTheRemainingGateway()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var circuitBreaker = new InMemoryGatewayCircuitBreaker(new GatewayFailoverOptions
        {
            CircuitFailureThreshold = 1,
            CircuitOpenSeconds = 60
        });
        circuitBreaker.RecordFailure("stripe"); // only stripe's circuit opens

        var sut = CreateSut([stripe, payPal], circuitBreaker: circuitBreaker);

        var result = await sut.ProcessPaymentAsync(ChargeRequest());

        result.Success.Should().BeTrue();
        result.ProviderId.Should().Be("paypal");
        stripe.ChargeCallCount.Should().Be(0, "the open circuit skips the gateway entirely");
        payPal.ChargeCallCount.Should().Be(1);
        _alerter.RaisedCircuitStates.Should().BeEmpty("a healthy gateway remained available");
    }

    [Fact]
    public async Task Charge_FailsClosed_WhenSelectionYieldsNoEligibleGateway()
    {
        var stripe = new FakePaymentGateway("stripe") { IsEnabled = false };
        var payPal = new FakePaymentGateway("paypal") { IsEnabled = false };
        var sut = CreateSut([stripe, payPal]);

        var act = () => sut.ProcessPaymentAsync(ChargeRequest());

        await act.Should().ThrowAsync<GatewaySelectionFailedException>()
            .WithMessage("*No eligible payment gateway*");
    }

    [Fact]
    public async Task Charge_RespectsCurrencyFiltersDuringFailover()
    {
        // Stripe serves USD only; a EUR charge selects PayPal alone even though Stripe
        // is registered and enabled.
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var routing = new GatewayRoutingOptions
        {
            Gateways = [new GatewayRouteEntry { ProviderId = "stripe", SupportedCurrencies = ["USD"] }]
        };
        var sut = CreateSut([stripe, payPal], routing: routing);

        var result = await sut.ProcessPaymentAsync(ChargeRequest(currency: "EUR"));

        result.Success.Should().BeTrue();
        result.ProviderId.Should().Be("paypal");
        stripe.ChargeCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Refund_IsPinnedToTheOwningGateway_AndRetriesOnlyThere()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, payPal]);

        var result = await sut.ProcessRefundAsync(new GatewayRefundRequest(
            "refund-1",
            "capture_9",
            Amount: 10m,
            Reason: "customer request",
            ProviderId: "paypal"));

        result.Success.Should().BeTrue();
        payPal.RefundCalls.Should().ContainSingle();
        stripe.RefundCalls.Should().BeEmpty("refunds never reroute to another gateway");
    }

    [Fact]
    public async Task Refund_FailsClosed_WhenOwnerProviderIsNotRegistered()
    {
        var sut = CreateSut([new FakePaymentGateway("stripe")]);

        var act = () => sut.ProcessRefundAsync(new GatewayRefundRequest(
            "refund-1",
            "capture_9",
            Amount: 10m,
            Reason: "customer request",
            ProviderId: "adyen"));

        await act.Should().ThrowAsync<GatewaySelectionFailedException>()
            .WithMessage("*'adyen'*not a registered gateway*");
    }

    [Fact]
    public async Task Inquiry_IsPinnedToTheOwningGateway()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var sut = CreateSut([stripe, payPal]);

        var result = await sut.GetPaymentAsync("capture_9", CancellationToken.None, providerId: "paypal");

        result.Success.Should().BeTrue();
        result.TransactionId.Should().Be("capture_9");
    }

    [Fact]
    public async Task ProviderId_ExposesThePrimaryGateway()
    {
        var sut = CreateSut([new FakePaymentGateway("paypal"), new FakePaymentGateway("stripe")]);

        sut.ProviderId.Should().Be("stripe");
        sut.DisplayName.Should().Be("Fake stripe");
        sut.IsEnabled.Should().BeTrue();
    }
}
