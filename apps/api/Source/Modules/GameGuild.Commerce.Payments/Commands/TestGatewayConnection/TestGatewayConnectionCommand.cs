using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Validates one gateway's API credentials with a cheap provider round trip (issue
///     #413): resolves the registered adapter's tester and executes it. Requires the
///     <c>payments:admin</c> permission. Fail-closed semantics: an unknown provider fails with
///     <see cref="GatewaySelectionFailedException" />; a disabled adapter or a provider without
///     a tester returns an explicit failed result — never a guessed success. No domain state
///     mutates — the operation event is the observable contract.
/// </summary>
/// <param name="ProviderId">Registered gateway provider identifier to test.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record TestGatewayConnectionCommand(string ProviderId) : ICommand<GatewayConnectionTestResult>;

/// <summary>
///     Handler for <see cref="TestGatewayConnectionCommand" />.
/// </summary>
public sealed class TestGatewayConnectionCommandHandler(
    IGatewayRegistry gatewayRegistry,
    IEnumerable<IGatewayConnectionTester> testers,
    ILogger<TestGatewayConnectionCommandHandler> logger) : ICommandHandler<TestGatewayConnectionCommand, GatewayConnectionTestResult>
{
    /// <inheritdoc />
    public async Task<GatewayConnectionTestResult> Handle(TestGatewayConnectionCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProviderId);

        // Fail closed: credentials can only be tested for a provider with a registered adapter.
        var gateway = gatewayRegistry.Find(request.ProviderId) ??
            throw new GatewaySelectionFailedException(
                $"The provider '{request.ProviderId}' is not a registered payment gateway; there is nothing to test.");

        var providerId = gateway.ProviderId;

        if (!gateway.IsEnabled)
        {
            // A disabled adapter is a legitimate, explicit test outcome — not an exception
            // and never a simulated pass.
            logger.LogInformation(
                "Gateway connection test for {ProviderId} skipped: the adapter is disabled",
                providerId);
            return new GatewayConnectionTestResult(
                providerId,
                Succeeded: false,
                Mode: "disabled",
                LatencyMs: 0,
                ErrorCode: "adapter_disabled",
                ErrorMessage: $"The {providerId} adapter is disabled (PaymentGateways configuration); no credentials were exercised.",
                CheckedAtUtc: SystemClock.UtcNow);
        }

        var tester = testers.FirstOrDefault(candidate =>
            string.Equals(candidate.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));

        if (tester is null)
        {
            logger.LogWarning(
                "Gateway connection test for {ProviderId} failed: no connection tester is registered",
                providerId);
            return new GatewayConnectionTestResult(
                providerId,
                Succeeded: false,
                Mode: "unsupported",
                LatencyMs: 0,
                ErrorCode: "connection_test_not_supported",
                ErrorMessage: $"No connection test is implemented for the {providerId} adapter.",
                CheckedAtUtc: SystemClock.UtcNow);
        }

        var result = await tester.TestConnectionAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Gateway connection test for {ProviderId} finished: {Outcome} ({LatencyMs} ms, mode {Mode})",
            providerId,
            result.Succeeded ? "succeeded" : $"failed with {result.ErrorCode}",
            result.LatencyMs,
            result.Mode);

        return result;
    }
}
