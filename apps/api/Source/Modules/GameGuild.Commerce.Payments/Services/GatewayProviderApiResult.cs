namespace GameGuild.Commerce.Payments;

/// <summary>
///     Outcome of one outbound REST call performed by a gateway adapter's HTTP client
///     (issue #413 adapter expansion — Authorize.Net, Braintree, Adyen). Mirrors the
///     PayPal client contract: transport failures, HTTP status and the raw body are
///     surfaced verbatim so each adapter can classify failures without the client guessing.
/// </summary>
/// <param name="Success">Whether the provider answered 2xx.</param>
/// <param name="StatusCode">HTTP status code when a response arrived; null on transport failure.</param>
/// <param name="Body">Raw response body when present.</param>
/// <param name="ErrorCode">Best-effort error code extracted from the body.</param>
/// <param name="ErrorMessage">Best-effort error message extracted from the body.</param>
/// <param name="TransportFailed">True when the request never produced a response (DNS, connection, timeout).</param>
public sealed record GatewayProviderApiResult(
    bool Success,
    int? StatusCode,
    string? Body,
    string? ErrorCode,
    string? ErrorMessage,
    bool TransportFailed)
{
    /// <summary>True when the call failed with an HTTP 408/429/5xx response (safe to retry or fail over).</summary>
    public bool IsServerError =>
        StatusCode is 408 or 429 or >= 500;

    /// <summary>True when the call failed with an HTTP 4xx response (terminal for automated retry).</summary>
    public bool IsClientError =>
        StatusCode is >= 400 and < 500 && !IsServerError;
}
