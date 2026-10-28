namespace GameGuild.Commerce.Payments;

/// <summary>
///     Outcome of one gateway connection test (issue #413): whether the gateway's API
///     credentials are usable, through the cheapest authenticated call the provider
///     offers. Never contains a credential value.
/// </summary>
/// <param name="ProviderId">Gateway provider identifier that was tested.</param>
/// <param name="Succeeded">Whether the credential check passed.</param>
/// <param name="Mode">Credential mode exercised: <c>simulation</c> or <c>live</c>.</param>
/// <param name="LatencyMs">Wall-clock duration of the provider round trip; 0 in simulation mode.</param>
/// <param name="ErrorCode">Stable error code when <paramref name="Succeeded" /> is false.</param>
/// <param name="ErrorMessage">Human-readable failure detail; never contains credentials.</param>
/// <param name="CheckedAtUtc">UTC instant the test ran.</param>
public sealed record GatewayConnectionTestResult(
    string ProviderId,
    bool Succeeded,
    string Mode,
    long LatencyMs,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime CheckedAtUtc);

/// <summary>
///     Connection test for one payment gateway (issue #413): performs a cheap authenticated
///     call against the provider API (for example Stripe <c>GET /v1/balance</c>, PayPal
///     OAuth2 token request) to validate that the configured credentials work. Implementations
///     resolve the effective credential through the encrypted credential store first and the
///     configuration fallback second, mirror the adapter's simulation disposition, and fail
///     closed with an explicit result — never a fake success.
/// </summary>
public interface IGatewayConnectionTester
{
    /// <summary>Provider identifier this tester covers (matches the registered adapter).</summary>
    string ProviderId { get; }

    /// <summary>Runs the credential check and returns its outcome.</summary>
    Task<GatewayConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}
