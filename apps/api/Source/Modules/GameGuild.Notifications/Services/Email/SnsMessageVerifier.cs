using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.SimpleNotificationService.Util;
using GameGuild.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Default <see cref="ISnsMessageVerifier"/> wrapping the AWS SDK's
/// <see cref="Message.ParseMessage(string)"/> + <see cref="Message.IsMessageSignatureValid"/>.
/// Both SDK calls THROW <see cref="Amazon.Runtime.AmazonClientException"/> (invalid SigningCertURL,
/// invalid SignatureVersion, cert download failure), so every step below maps exceptions to a
/// typed rejection: hostile input must surface as 401/400 at the webhook, never an unhandled 500.
/// Envelope shape and signature-trust fields are checked on the raw JSON BEFORE delegating,
/// because ParseMessage itself throws on those — keeping Malformed (400) reserved for
/// non-JSON/incomplete bodies and Signature (401) for anything the trust chain rejects.
/// </summary>
public sealed partial class SnsMessageVerifier(
    IOptions<EmailDeliveryOptions> options,
    IHostEnvironment environment,
    ILogger<SnsMessageVerifier> logger) : ISnsMessageVerifier
{
    // AWS regional SNS signing cert hosts, e.g. sns.us-east-1.amazonaws.com. Pinned before
    // any SDK delegation (defense-in-depth: the SDK also validates, but only after we do).
    [GeneratedRegex(@"^sns\.[a-z0-9-]+\.amazonaws\.com$")]
    private static partial Regex SigningCertHostRegex();

    // Process-wide "warn once" for the dev-only unconfigured-topic case (static: verifier is scoped).
    private static int _topicNotConfiguredWarned;

    /// <inheritdoc />
    public SnsVerificationResult ValidateRequest(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (GetString(root, "Type") is null || GetString(root, "MessageId") is null || GetString(root, "Message") is null)
            {
                return Invalid(SnsRejectionReason.Malformed, "SNS envelope is missing required fields.");
            }

            // Topic rule first (a plain string compare — fail fast before any signature work).
            var configuredTopicArn = options.Value.Events.TopicArn;
            if (!string.IsNullOrWhiteSpace(configuredTopicArn))
            {
                if (!string.Equals(GetString(root, "TopicArn"), configuredTopicArn, StringComparison.Ordinal))
                {
                    return Invalid(SnsRejectionReason.TopicMismatch, "Envelope topic does not match the configured topic.");
                }
            }
            else if (environment.IsProduction())
            {
                // Plan guardrail: the webhook rejects ALL events in production unless a topic ARN is configured.
                return Invalid(SnsRejectionReason.TopicNotConfigured, "No topic ARN configured; rejecting in production.");
            }
            else if (Interlocked.Exchange(ref _topicNotConfiguredWarned, 1) == 0)
            {
                logger.LogWarning("EmailDelivery:Events:TopicArn is not configured; accepting SNS email events from any topic (non-production only).");
            }

            // Signature-trust pre-checks. IsMessageSignatureValid downloads the SigningCertURL,
            // so the host is pinned here, before any fetch; a bad SignatureVersion (SDK accepts
            // only "1"/"2") or missing signature means the body can never be trusted.
            if (!IsTrustedAwsSnsUrl(GetString(root, "SigningCertURL")))
            {
                return Invalid(SnsRejectionReason.Signature, "Signing certificate URL is not a trusted AWS SNS endpoint.");
            }

            if (GetString(root, "SignatureVersion") is not ("1" or "2") || GetString(root, "Signature") is null)
            {
                return Invalid(SnsRejectionReason.Signature, "SNS envelope carries no verifiable signature.");
            }
        }
        catch (Exception)
        {
            return Invalid(SnsRejectionReason.Malformed, "Body is not a parseable SNS envelope.");
        }

        Message envelope;
        try
        {
            envelope = Message.ParseMessage(body);
        }
        catch (Exception)
        {
            return Invalid(SnsRejectionReason.Malformed, "Body is not a parseable SNS envelope.");
        }

        try
        {
            if (!envelope.IsMessageSignatureValid())
            {
                return Invalid(SnsRejectionReason.Signature, "SNS message signature is not valid.");
            }
        }
        catch (Exception)
        {
            // AmazonClientException on cert download failure, crypto errors, ...
            return Invalid(SnsRejectionReason.Signature, "SNS message signature could not be validated.");
        }

        return new SnsVerificationResult.Valid(envelope);
    }

    /// <summary>
    /// True when <paramref name="url"/> is an absolute https URL whose host is a regional
    /// AWS SNS host (<c>sns.*.amazonaws.com</c>). Shared by the webhook for both the
    /// SigningCertURL pin and the SubscribeURL SSRF guard — host is validated BEFORE any fetch.
    /// </summary>
    public static bool IsTrustedAwsSnsUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && SigningCertHostRegex().IsMatch(uri.Host);
    }

    private static SnsVerificationResult.Invalid Invalid(SnsRejectionReason rejection, string reason) =>
        new(rejection, reason);

    /// <summary>Returns the string value of <paramref name="name"/> trimmed of whitespace, or null when absent/blank/non-string.</summary>
    private static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } text
        && !string.IsNullOrWhiteSpace(text)
            ? text
            : null;
}
