namespace GameGuild.Commerce.Billing;

/// <summary>
///     Per-request delivery source of a billing webhook callback (issue #408: IP address and
///     user agent logging). Populated by <see cref="WebhookSourceSecurityFilter"/> — which runs
///     on every provider callback endpoint before the payload is read — and stamped onto the
///     durable inbox row by the webhook repository when the event is first persisted. Outside a
///     provider callback request (for example the retry worker) the context stays empty.
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
    ///     Captures the delivery source of the current callback request. First capture wins:
    ///     later captures (for example a second filter pass) never overwrite the original.
    /// </summary>
    void Capture(string? provider, string? sourceIpAddress, string? userAgent);
}

/// <inheritdoc />
public sealed class WebhookDeliveryContext : IWebhookDeliveryContext
{
    /// <inheritdoc />
    public string? Provider { get; private set; }

    /// <inheritdoc />
    public string? SourceIpAddress { get; private set; }

    /// <inheritdoc />
    public string? UserAgent { get; private set; }

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
}
