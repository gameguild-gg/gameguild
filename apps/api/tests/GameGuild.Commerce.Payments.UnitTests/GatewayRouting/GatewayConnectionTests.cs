using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.GatewayRouting;

/// <summary>
///     Gateway connection tests (issue #413): the query handler fails closed on unknown
///     providers, reports disabled adapters and tester-less providers as explicit failures,
///     dispatches to the matching tester, and the adapter testers mirror the simulation
///     disposition and fail closed on missing credentials.
/// </summary>
public class GatewayConnectionTests
{
    private sealed class FakeConnectionTester(string providerId, Func<GatewayConnectionTestResult> outcome)
        : IGatewayConnectionTester
    {
        public string ProviderId { get; } = providerId;

        public Task<GatewayConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(outcome());
    }

    private sealed class EnabledGateway(string providerId, bool enabled = true) : IPaymentGateway
    {
        public string ProviderId { get; } = providerId;
        public string DisplayName => $"Fake {ProviderId}";
        public bool IsEnabled => enabled;

        public Task<GatewayPaymentResult> ProcessPaymentAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayPaymentResult> GetPaymentAsync(string externalTransactionId, CancellationToken cancellationToken = default, string? providerId = null) =>
            throw new NotSupportedException();

        public Task<GatewayPaymentCancellationResult> CancelPaymentAsync(string externalTransactionId, CancellationToken cancellationToken = default, string? providerId = null) =>
            throw new NotSupportedException();

        public Task<GatewayRefundResult> ProcessRefundAsync(GatewayRefundRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayDisputeActionResult> AcceptDisputeAsync(GatewayDisputeActionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayDisputeActionResult> ChallengeDisputeAsync(GatewayDisputeChallengeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret) =>
            Task.FromResult(false);

        public Task<GatewayCustomerResult> CreateCustomerAsync(GatewayCustomerRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayPaymentMethodResult> CreatePaymentMethodAsync(GatewayPaymentMethodRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayCancellationResult> CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class Registry(params IPaymentGateway[] gateways) : IGatewayRegistry
    {
        public IReadOnlyList<IPaymentGateway> Gateways { get; } = gateways;

        public IPaymentGateway? Find(string providerId) =>
            Gateways.FirstOrDefault(gateway =>
                string.Equals(gateway.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
    }

    private static TestGatewayConnectionCommandHandler CreateHandler(
        IGatewayRegistry registry,
        params IGatewayConnectionTester[] testers) =>
        new(registry, testers, NullLogger<TestGatewayConnectionCommandHandler>.Instance);

    [Fact]
    public async Task UnknownProvider_FailsClosed()
    {
        var handler = CreateHandler(new Registry(new EnabledGateway("stripe")));

        var act = () => handler.Handle(new TestGatewayConnectionCommand("adyen"), CancellationToken.None);

        await act.Should().ThrowAsync<GatewaySelectionFailedException>()
            .WithMessage("*not a registered payment gateway*");
    }

    [Fact]
    public async Task DisabledAdapter_ReturnsAnExplicitFailedResult()
    {
        var handler = CreateHandler(
            new Registry(new EnabledGateway("stripe", enabled: false)),
            new FakeConnectionTester("stripe", () => throw new InvalidOperationException("must not be called")));

        var result = await handler.Handle(new TestGatewayConnectionCommand("stripe"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("adapter_disabled");
        result.ProviderId.Should().Be("stripe");
    }

    [Fact]
    public async Task ProviderWithoutTester_ReturnsAnExplicitFailedResult()
    {
        var handler = CreateHandler(new Registry(new EnabledGateway("paypal")));

        var result = await handler.Handle(new TestGatewayConnectionCommand("paypal"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("connection_test_not_supported");
    }

    [Fact]
    public async Task RegisteredTester_IsDispatched_ByProviderId()
    {
        var pass = new GatewayConnectionTestResult(
            "adyen", Succeeded: true, Mode: "live", LatencyMs: 42, ErrorCode: null, ErrorMessage: null, CheckedAtUtc: SystemClock.UtcNow);
        var handler = CreateHandler(
            new Registry(new EnabledGateway("adyen")),
            new FakeConnectionTester("adyen", () => pass));

        var result = await handler.Handle(new TestGatewayConnectionCommand("ADYEN"), CancellationToken.None);

        result.Should().BeSameAs(pass);
    }

    [Fact]
    public async Task StripeTester_SimulationMode_ReturnsAnExplicitSimulatedPass()
    {
        var tester = new StripeGatewayConnectionTester(
            Options.Create(new StripeGatewayOptions { IsEnabled = true, UseSimulation = true }),
            new ResolverReturning(null));

        var result = await tester.TestConnectionAsync();

        result.Succeeded.Should().BeTrue();
        result.Mode.Should().Be("simulation");
        result.LatencyMs.Should().Be(0);
    }

    [Fact]
    public async Task StripeTester_LiveMode_FailsClosedWithoutCredentials()
    {
        var tester = new StripeGatewayConnectionTester(
            Options.Create(new StripeGatewayOptions { IsEnabled = true, UseSimulation = false, ApiKey = "" }),
            new ResolverReturning(null));

        var result = await tester.TestConnectionAsync();

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("stripe_credentials_missing");
    }

    [Fact]
    public async Task StripeTester_LiveMode_ProbesTheBalanceEndpointWithTheResolvedKey()
    {
        string? probedKey = null;
        var tester = new StripeGatewayConnectionTester(
            Options.Create(new StripeGatewayOptions { IsEnabled = true, UseSimulation = false, ApiKey = "config-key" }),
            new ResolverReturning("stored-key"))
        {
            BalanceProbe = (apiKey, _) =>
            {
                probedKey = apiKey;
                return Task.CompletedTask;
            }
        };

        var result = await tester.TestConnectionAsync();

        result.Succeeded.Should().BeTrue();
        result.Mode.Should().Be("live");
        probedKey.Should().Be("stored-key", "the encrypted-store credential wins over the configuration fallback");
    }

    [Fact]
    public async Task PayPalTester_LiveMode_ClassifiesTheHttpStatus()
    {
        var tester = new PayPalGatewayConnectionTester(
            Options.Create(new PayPalGatewayOptions { IsEnabled = true, UseSimulation = false, ClientId = "id", ClientSecret = "secret" }),
            new ResolverReturning(null))
        {
            TokenProbe = (_, _, _, _) => Task.FromResult<int?>(401)
        };

        var result = await tester.TestConnectionAsync();

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("paypal_http_401");

        var passing = new PayPalGatewayConnectionTester(
            Options.Create(new PayPalGatewayOptions { IsEnabled = true, UseSimulation = false, ClientId = "id", ClientSecret = "secret" }),
            new ResolverReturning(null))
        {
            TokenProbe = (_, _, _, _) => Task.FromResult<int?>(200)
        };

        (await passing.TestConnectionAsync()).Succeeded.Should().BeTrue();
    }

    private sealed class ResolverReturning(string? plaintext) : IGatewayCredentialResolver
    {
        public Task<string?> ResolveAsync(
            string providerKey,
            string secretName,
            string? configurationFallback = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(plaintext ?? configurationFallback);
    }
}
