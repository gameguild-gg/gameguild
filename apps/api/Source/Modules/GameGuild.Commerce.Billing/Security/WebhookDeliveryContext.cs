namespace GameGuild.Commerce.Billing;

/// <summary>
///     Per-request delivery source of a billing webhook callback (issue #408: IP address and
///     user agent logging). Populated by <see cref="WebhookSourceSecurityFilter"/> — which runs
///     on every provider callback endpoint before the payload is read — and stamped onto the
///     durable inbox row by the webhook repository when the event is first persisted. Outside a
///     provider callback request (for example the retry worker) the context stays empty.
///     The context also carries the opt-in RAW delivery capture (issue #408: capture raw
///     payload and all headers): the controller snapshots the raw body plus the full header
///     set once it has read them, and the webhook repository persists the redacted form —
///     only after signature verification — when <see cref="WebhookRetentionPolicy"/> is
///     enabled. Raw bytes live in this scoped context for the duration of the request and
///     are never stored unredacted.
/// </summary>
public interface IWebhookDeliveryContext
{
    /// <summary>Provider key resolved from the callback route, when known.</summary>
    string? Provider { get; }

    /// <summary>Remote IP address the callback was accepted from, when known.</summary>
    string? SourceIpAddress { get; }

    /// <summary>Bounded user agent of the callback request, when present.</summary>
    string? UserAgent { get; }

    /// <summary>
    ///     Raw HTTP body of the callback, captured by the endpoint action, when raw-delivery
    ///     retention is enabled. Bounded to <see cref="WebhookDeliveryContext.MaxRawPayloadLength"/>.
    /// </summary>
    string? RawPayload { get; }

    /// <summary>
    ///     Full header set of the callback request, captured by the endpoint action, when
    ///     raw-delivery retention is enabled.
    /// </summary>
    IReadOnlyList<KeyValuePair<string, string>>? RawHeaders { get; }

    /// <summary>
    ///     Captures the delivery source of the current callback request. First capture wins:
    ///     later captures (for example a second filter pass) never overwrite the original.
    /// </summary>
    void Capture(string? provider, string? sourceIpAddress, string? userAgent);

    /// <summary>
    ///     Captures the raw body and full header set of the current callback request for the
    ///     opt-in raw-delivery retention (issue #408). First capture wins. The snapshot is
    ///     bounded (payload length, header count and value length) so a hostile delivery
    ///     cannot balloon request-scoped memory. The redaction pass happens at persistence
    ///     time, after signature verification.
    /// </summary>
    void CaptureRawDelivery(string? rawPayload, IReadOnlyDictionary<string, string>? rawHeaders);
}

/// <inheritdoc />
public sealed class WebhookDeliveryContext : IWebhookDeliveryContext
{
    /// <summary>Ceiling on the captured raw payload length (256 KB).</summary>
    public const int MaxRawPayloadLength = 256 * 1024;

    /// <summary>Ceiling on the number of captured headers.</summary>
    public const int MaxRawHeaderCount = 100;

    /// <summary>Ceiling on a single captured header value's length.</summary>
    public const int MaxRawHeaderValueLength = 8192;

    /// <inheritdoc />
    public string? Provider { get; private set; }

    /// <inheritdoc />
    public string? SourceIpAddress { get; private set; }

    /// <inheritdoc />
    public string? UserAgent { get; private set; }

    /// <inheritdoc />
    public string? RawPayload { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, string>>? RawHeaders { get; private set; }

    /// <inheritdoc />
    public void Capture(string? provider, string? sourceIpAddress, string? userAgent)
    {
        if (!string.IsNullOrWhiteSpace(provider))
        {
            Provider ??= provider.Trim();
        }

        if (!string.IsNullOrWhiteSpace(sourceIpAddress))
        {
            SourceIpAddress ??= sourceIpAddress.Trim();
        }

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            var agent = userAgent.Trim();
            UserAgent ??= (agent.Length <= 512 ? agent : agent[..512]);
        }
    }

    /// <inheritdoc />
    public void CaptureRawDelivery(string? rawPayload, IReadOnlyDictionary<string, string>? rawHeaders)
    {
        if (RawPayload is not null || RawHeaders is not null)
        {
            // First capture wins, mirroring Capture: a second capture pass never
            // overwrites the original delivery evidence.
            return;
        }

        if (!string.IsNullOrEmpty(rawPayload))
        {
            RawPayload = rawPayload.Length <= MaxRawPayloadLength ? rawPayload : rawPayload[..MaxRawPayloadLength];
        }

        if (rawHeaders is { Count: > 0 })
        {
            RawHeaders = rawHeaders
                .Take(MaxRawHeaderCount)
                .Select(header => new KeyValuePair<string, string>(
                    header.Key,
                    header.Value.Length <= MaxRawHeaderValueLength ? header.Value : header.Value[..MaxRawHeaderValueLength]))
                .ToList();
        }
    }
}
