using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.GatewayRouting;

/// <summary>
///     Adapter contract tests for the Authorize.Net, Braintree and Adyen gateways (issue
///     #413): fail-closed defaults (disabled adapters answer every operation with an explicit
///     failure, never a fake success), deterministic simulation results, real-mode result
///     mapping with the documented failure classification (transport/5xx retriable, 4xx
///     terminal, unconfirmed outcomes outcome-unknown), and the credential-validating
///     connection tests.
/// </summary>
public class AdditionalGatewayAdapterTests
{
    private const string Descriptor = "COMMON.ACCEPT.INFINITE.PAYMENT";
    private const string OpaqueValue = "$opaque$1234567890";

    private static GatewayPaymentRequest Charge(string paymentMethodId) =>
        new("idem-1", 25m, "USD", "cus_1", paymentMethodId, "test charge");

    [Fact]
    public async Task AuthorizeNet_FailsClosed_WhenDisabled()
    {
        var adapter = new AuthorizeNetPaymentGateway(
            Options.Create(new AuthorizeNetGatewayOptions()),
            new Mock<IAuthorizeNetApiClient>().Object);

        adapter.IsEnabled.Should().BeFalse("the adapter is disabled until PaymentGateways:AuthorizeNet opts in");

        var charge = await adapter.ProcessPaymentAsync(Charge("COMMON.ACCEPT.INFINITE.PAYMENT|$opaque$"));
        charge.Success.Should().BeFalse();
        charge.ErrorCode.Should().Be("authorizenet_adapter_disabled");
        charge.FailureKind.Should().Be(GatewayFailureKind.TerminalDecline);

        var refund = await adapter.ProcessRefundAsync(new GatewayRefundRequest("r1", "txn_1", 10m, "customer request"));
        refund.Success.Should().BeFalse();
        refund.ErrorCode.Should().Be("authorizenet_adapter_disabled");
    }

    [Fact]
    public async Task AuthorizeNet_SimulationMode_ChargesAndRefundsDeterministically()
    {
        var adapter = new AuthorizeNetPaymentGateway(
            Options.Create(new AuthorizeNetGatewayOptions { IsEnabled = true, UseSimulation = true }),
            new Mock<IAuthorizeNetApiClient>().Object);

        var charge = await adapter.ProcessPaymentAsync(Charge($"{Descriptor}|{OpaqueValue}"));
        charge.Success.Should().BeTrue();
        charge.TransactionId.Should().StartWith("ANET-SIMULATED-");
        charge.ProviderMapping.Should().NotBeNull();
        charge.ProviderMapping!.ProviderEnvironment.Should().Be("test");

        var refund = await adapter.ProcessRefundAsync(new GatewayRefundRequest("r1", charge.TransactionId!, 25m, "full refund"));
        refund.Success.Should().BeTrue();
        refund.RefundId.Should().StartWith("ANET-REFUND-SIMULATED-");

        (await adapter.TestConnectionAsync()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeNet_RealMode_MapsApprovalDeclineAndTransportFailures()
    {
        var client = new Mock<IAuthorizeNetApiClient>();
        var adapter = new AuthorizeNetPaymentGateway(
            Options.Create(new AuthorizeNetGatewayOptions { IsEnabled = true }),
            client.Object);

        client.Setup(instance => instance.ChargeAsync("idem-1", 25m, "USD", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("""{"transactionResponse":{"transId":"4001","responseCode":"1"},"messages":{"resultCode":"Ok"}}"""));
        (await adapter.ProcessPaymentAsync(Charge($"{Descriptor}|{OpaqueValue}")))
            .Should().Match<GatewayPaymentResult>(result => result.Success && result.TransactionId == "4001");

        client.Setup(instance => instance.ChargeAsync("idem-1", 25m, "USD", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("""{"transactionResponse":{"transId":"4002","responseCode":"2"},"messages":{"resultCode":"Error"}}"""));
        (await adapter.ProcessPaymentAsync(Charge($"{Descriptor}|{OpaqueValue}")))
            .Should().Match<GatewayPaymentResult>(result =>
                !result.Success && result.FailureKind == GatewayFailureKind.TerminalDecline);

        client.Setup(instance => instance.ChargeAsync("idem-1", 25m, "USD", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayProviderApiResult(false, 503, null, "authorizenet_http_503", "server error", TransportFailed: false));
        (await adapter.ProcessPaymentAsync(Charge($"{Descriptor}|{OpaqueValue}")))
            .Should().Match<GatewayPaymentResult>(result =>
                !result.Success && result.FailureKind == GatewayFailureKind.RetriableGatewayFailure);
    }

    [Fact]
    public async Task Braintree_FailsClosed_WhenDisabled()
    {
        var adapter = new BraintreePaymentGateway(
            Options.Create(new BraintreeGatewayOptions()),
            new Mock<IBraintreeApiClient>().Object);

        adapter.IsEnabled.Should().BeFalse();

        var charge = await adapter.ProcessPaymentAsync(Charge("tokencc_bh_sandbox_abc123"));
        charge.Success.Should().BeFalse();
        charge.ErrorCode.Should().Be("braintree_adapter_disabled");
    }

    [Fact]
    public async Task Braintree_SimulationMode_ChargesDeterministically()
    {
        var adapter = new BraintreePaymentGateway(
            Options.Create(new BraintreeGatewayOptions { IsEnabled = true, UseSimulation = true }),
            new Mock<IBraintreeApiClient>().Object);

        var charge = await adapter.ProcessPaymentAsync(Charge("tokencc_bh_sandbox_abc123"));
        charge.Success.Should().BeTrue();
        charge.TransactionId.Should().StartWith("BT-SIMULATED-");

        (await adapter.TestConnectionAsync()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Braintree_RealMode_MapsSettlementTerminalDeclineAndTransport()
    {
        var client = new Mock<IBraintreeApiClient>();
        var adapter = new BraintreePaymentGateway(
            Options.Create(new BraintreeGatewayOptions { IsEnabled = true }),
            client.Object);

        client.Setup(instance => instance.SaleAsync("idem-1", 25m, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("""<transaction><id>bt_txn_1</id><status>submitted_for_settlement</status></transaction>"""));
        (await adapter.ProcessPaymentAsync(Charge("tokencc_bh_sandbox_abc123")))
            .Should().Match<GatewayPaymentResult>(result => result.Success && result.TransactionId == "bt_txn_1");

        client.Setup(instance => instance.SaleAsync("idem-1", 25m, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("""<transaction><id>bt_txn_2</id><status>processor_declined</status></transaction>"""));
        (await adapter.ProcessPaymentAsync(Charge("tokencc_bh_sandbox_abc123")))
            .Should().Match<GatewayPaymentResult>(result =>
                !result.Success && result.FailureKind == GatewayFailureKind.TerminalDecline);

        client.Setup(instance => instance.SaleAsync("idem-1", 25m, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayProviderApiResult(false, null, null, "braintree_transport_failure", "timeout", TransportFailed: true));
        (await adapter.ProcessPaymentAsync(Charge("tokencc_bh_sandbox_abc123")))
            .Should().Match<GatewayPaymentResult>(result =>
                !result.Success && result.FailureKind == GatewayFailureKind.RetriableGatewayFailure);
    }

    [Fact]
    public async Task Adyen_FailsClosed_WhenDisabled()
    {
        var adapter = new AdyenPaymentGateway(
            Options.Create(new AdyenGatewayOptions()),
            new Mock<IAdyenApiClient>().Object);

        adapter.IsEnabled.Should().BeFalse();

        var charge = await adapter.ProcessPaymentAsync(Charge("KL4TW4V5KXQZ6W3H"));
        charge.Success.Should().BeFalse();
        charge.ErrorCode.Should().Be("adyen_adapter_disabled");

        // The Checkout API exposes no inquiry; even enabled, the adapter fails closed
        // instead of fabricating a result.
        var enabled = new AdyenPaymentGateway(
            Options.Create(new AdyenGatewayOptions { IsEnabled = true, UseSimulation = true }),
            new Mock<IAdyenApiClient>().Object);
        var inquiry = await enabled.GetPaymentAsync("KL4TW4V5KXQZ6W3H");
        inquiry.Success.Should().BeFalse();
        inquiry.ErrorCode.Should().Be("adyen_inquiry_not_supported");
    }

    [Fact]
    public async Task Adyen_SimulationMode_ChargesAndRefundsDeterministically()
    {
        var adapter = new AdyenPaymentGateway(
            Options.Create(new AdyenGatewayOptions { IsEnabled = true, UseSimulation = true }),
            new Mock<IAdyenApiClient>().Object);

        var charge = await adapter.ProcessPaymentAsync(Charge("KL4TW4V5KXQZ6W3H"));
        charge.Success.Should().BeTrue();
        charge.TransactionId.Should().StartWith("ADYEN-SIMULATED-");

        var refund = await adapter.ProcessRefundAsync(new GatewayRefundRequest("r1", charge.TransactionId!, 25m, "full refund"));
        refund.Success.Should().BeTrue();

        (await adapter.TestConnectionAsync()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Adyen_RealMode_MapsAuthorisedRefusedAndUnconfirmedOutcomes()
    {
        var client = new Mock<IAdyenApiClient>();
        var adapter = new AdyenPaymentGateway(
            Options.Create(new AdyenGatewayOptions { IsEnabled = true }),
            client.Object);

        client.Setup(instance => instance.PaymentAsync("idem-1", 25m, "USD", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("""{"pspReference":"8525","resultCode":"Authorised"}"""));
        (await adapter.ProcessPaymentAsync(Charge("KL4TW4V5KXQZ6W3H")))
            .Should().Match<GatewayPaymentResult>(result => result.Success && result.TransactionId == "8525");

        client.Setup(instance => instance.PaymentAsync("idem-1", 25m, "USD", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("""{"pspReference":"8526","resultCode":"Refused","refusalReason":"DECLINED_BY_ISSUER"}"""));
        (await adapter.ProcessPaymentAsync(Charge("KL4TW4V5KXQZ6W3H")))
            .Should().Match<GatewayPaymentResult>(result =>
                !result.Success && result.FailureKind == GatewayFailureKind.TerminalDecline);

        client.Setup(instance => instance.PaymentAsync("idem-1", 25m, "USD", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("{}"));
        (await adapter.ProcessPaymentAsync(Charge("KL4TW4V5KXQZ6W3H")))
            .Should().Match<GatewayPaymentResult>(result =>
                !result.Success && result.FailureKind == GatewayFailureKind.OutcomeUnknown);

        client.Setup(instance => instance.PaymentAsync("idem-1", 25m, "USD", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayProviderApiResult(false, 429, null, "adyen_http_429", "rate limited", TransportFailed: false));
        (await adapter.ProcessPaymentAsync(Charge("KL4TW4V5KXQZ6W3H")))
            .Should().Match<GatewayPaymentResult>(result =>
                !result.Success && result.FailureKind == GatewayFailureKind.RetriableGatewayFailure);
    }

    [Fact]
    public async Task ConnectionTests_ReportCredentialFailuresFromTheApiProbe()
    {
        var anetClient = new Mock<IAuthorizeNetApiClient>();
        anetClient.Setup(instance => instance.GetMerchantDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayProviderApiResult(false, 401, null, "authorizenet_http_401", "authentication failed", TransportFailed: false));
        var anet = new AuthorizeNetPaymentGateway(
            Options.Create(new AuthorizeNetGatewayOptions { IsEnabled = true }),
            anetClient.Object);

        var anetResult = await anet.TestConnectionAsync();
        anetResult.Succeeded.Should().BeFalse();
        anetResult.ErrorCode.Should().Be("authorizenet_http_401");

        var braintreeClient = new Mock<IBraintreeApiClient>();
        braintreeClient.Setup(instance => instance.CreateClientTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("<client-token/>"));
        var braintree = new BraintreePaymentGateway(
            Options.Create(new BraintreeGatewayOptions { IsEnabled = true }),
            braintreeClient.Object);

        (await braintree.TestConnectionAsync()).Succeeded.Should().BeTrue();

        var adyenClient = new Mock<IAdyenApiClient>();
        adyenClient.Setup(instance => instance.GetPaymentMethodsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result("""{"paymentMethods":[{"type":"scheme"}]}"""));
        var adyen = new AdyenPaymentGateway(
            Options.Create(new AdyenGatewayOptions { IsEnabled = true }),
            adyenClient.Object);

        (await adyen.TestConnectionAsync()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Options_RequireCredentials_WhenEnabledWithoutSimulation()
    {
        var anet = new AuthorizeNetGatewayOptionsValidator().Validate(null, new AuthorizeNetGatewayOptions { IsEnabled = true });
        anet.Failed.Should().BeTrue();
        anet.Failures.Should().Contain(message => message.Contains("ApiLoginId"));

        var braintree = new BraintreeGatewayOptionsValidator().Validate(null, new BraintreeGatewayOptions { IsEnabled = true });
        braintree.Failed.Should().BeTrue();
        braintree.Failures.Should().Contain(message => message.Contains("MerchantId"));

        var adyen = new AdyenGatewayOptionsValidator().Validate(null, new AdyenGatewayOptions { IsEnabled = true });
        adyen.Failed.Should().BeTrue();
        adyen.Failures.Should().Contain(message => message.Contains("ApiKey"));

        var simulationOnly = new AdyenGatewayOptionsValidator().Validate(
            null,
            new AdyenGatewayOptions { IsEnabled = true, UseSimulation = true });
        simulationOnly.Succeeded.Should().BeTrue("simulation mode needs no credentials");
    }

    [Fact]
    public void TokenClassifier_AttributesTokensToTheirGateway()
    {
        GatewayPaymentMethodTokens.ResolveProvider("pm_card_visa").Should().Be("stripe");
        GatewayPaymentMethodTokens.ResolveProvider("ba_xxxxxxxx").Should().Be("paypal");
        GatewayPaymentMethodTokens.ResolveProvider("B-12345678").Should().Be("paypal");
        GatewayPaymentMethodTokens.ResolveProvider($"{Descriptor}|{OpaqueValue}").Should().Be("authorizenet");
        GatewayPaymentMethodTokens.ResolveProvider("tokencc_bh_sandbox_abc123", "braintree").Should().Be("braintree");
        GatewayPaymentMethodTokens.ResolveProvider("KL4TW4V5KXQZ6W3H", "adyen").Should().Be("adyen");
        GatewayPaymentMethodTokens.ResolveProvider("tokencc_bh_sandbox_abc123").Should().BeNull();
        GatewayPaymentMethodTokens.ResolveProvider("KL4TW4V5KXQZ6W3H").Should().BeNull();
        GatewayPaymentMethodTokens.ResolveProvider("not_a_pm").Should().BeNull();
        GatewayPaymentMethodTokens.ResolveProvider("pm_card_visa", "adyen").Should().BeNull();
        GatewayPaymentMethodTokens.ResolveProvider("4111111111111111", "adyen").Should().BeNull();
        GatewayPaymentMethodTokens.ResolveProvider("4111-1111-1111-1111", "braintree").Should().BeNull();
        GatewayPaymentMethodTokens.ResolveProvider("COMMON.ACCEPT.INAPP.PAYMENT").Should().BeNull();

        GatewayPaymentMethodTokens.ResolveProvider("4111111111111111").Should().BeNull("raw card numbers never validate");
        GatewayPaymentMethodTokens.ResolveProvider("not a token!").Should().BeNull();
        GatewayPaymentMethodTokens.IsKnownGatewayToken("4111111111111111").Should().BeFalse();
    }

    [Fact]
    public void InferPaymentMethodType_TypesTheNewGatewayTokens()
    {
        GatewayRouteContext.InferPaymentMethodType("pm_card_visa").Should().Be("card");
        GatewayRouteContext.InferPaymentMethodType("ba_agreement").Should().Be("paypal");
        GatewayRouteContext.InferPaymentMethodType("tokencc_bh_sandbox_abc").Should().Be("card");
        GatewayRouteContext.InferPaymentMethodType("KL4TW4V5KXQZ6W3H").Should().Be("card");
        GatewayRouteContext.InferPaymentMethodType($"{Descriptor}|{OpaqueValue}").Should().Be("card");
    }

    private static GatewayProviderApiResult Result(string body) =>
        new(true, 200, body, null, null, TransportFailed: false);
}
