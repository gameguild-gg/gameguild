using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Offline Braintree webhook signature verification (issue #408). Follows Braintree's
///     documented delivery format: the <c>bt_signature</c> form field is
///     <c>{public key}|{sha1 hex of (public key + "|" + bt_payload)}</c>. The service checks
///     the signing key against the configured public key, recomputes the digest and compares
///     both in constant time, then base64-decodes the payload into the typed notification
///     (classic XML subjects and GraphQL JSON subjects are both understood). Fail-closed: an
///     unconfigured key, malformed input or undecodable payload never verifies.
/// </summary>
public sealed class BraintreeWebhookVerificationService(
    IOptions<BraintreeSettings> options,
    ILogger<BraintreeWebhookVerificationService> logger) : IBraintreeWebhookVerificationService
{
    private const int MaxSignatureLength = 512;
    private const int MaxPayloadLength = 1_000_000;

    private readonly BraintreeSettings _settings = options.Value;

    /// <inheritdoc />
    public BraintreeWebhookVerificationResult Verify(string btSignature, string btPayload)
    {
        if (string.IsNullOrWhiteSpace(_settings.PublicKey))
        {
            // Fail closed: without the gateway's public key nothing can be authenticated.
            logger.LogWarning("Braintree webhook verification is not configured; delivery rejected");
            return BraintreeWebhookVerificationResult.Failed("Braintree webhook verification is not configured");
        }

        if (string.IsNullOrWhiteSpace(btSignature) || btSignature.Length > MaxSignatureLength)
        {
            return BraintreeWebhookVerificationResult.Failed("bt_signature is missing or malformed");
        }

        if (string.IsNullOrEmpty(btPayload) || btPayload.Length > MaxPayloadLength)
        {
            return BraintreeWebhookVerificationResult.Failed("bt_payload is missing or exceeds the size bound");
        }

        // bt_signature = "{signing public key}|{digest}". Split on the first separator only:
        // the key itself never contains '|', but a hostile digest section may.
        var separatorIndex = btSignature.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == btSignature.Length - 1)
        {
            return BraintreeWebhookVerificationResult.Failed("bt_signature is not in key|digest form");
        }

        var signingKey = btSignature[..separatorIndex];
        var presentedDigest = btSignature[(separatorIndex + 1)..].Trim().ToLowerInvariant();

        if (!FixedTimeEqualsUtf8(signingKey, _settings.PublicKey))
        {
            logger.LogWarning("Braintree webhook signed by an unexpected public key; delivery rejected");
            return BraintreeWebhookVerificationResult.Failed("Signature key does not match this endpoint");
        }

        // Recompute the digest over the CONFIGURED key (already proven equal to the signing
        // key), never over attacker-controlled input beyond the verified equality.
        var expectedDigest = Sha1HexDigest(_settings.PublicKey + "|" + btPayload);

        if (!FixedTimeEqualsUtf8(presentedDigest, expectedDigest))
        {
            logger.LogWarning("Braintree webhook signature digest mismatch; delivery rejected");
            return BraintreeWebhookVerificationResult.Failed("Signature digest mismatch");
        }

        BraintreeWebhookNotification notification;
        try
        {
            notification = DecodeNotification(btPayload);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Verified Braintree webhook payload could not be decoded");
            return BraintreeWebhookVerificationResult.Failed("Payload could not be decoded");
        }

        if (string.IsNullOrWhiteSpace(notification.Kind))
        {
            return BraintreeWebhookVerificationResult.Failed("Payload carries no notification kind");
        }

        return BraintreeWebhookVerificationResult.Success(notification);
    }

    /// <summary>Decodes the base64 payload and projects it onto the routing model.</summary>
    internal static BraintreeWebhookNotification DecodeNotification(string btPayload)
    {
        var decoded = DecodeBase64Flexible(btPayload);
        var trimmed = decoded.TrimStart();

        return trimmed.StartsWith('<') ? ParseXmlNotification(decoded) : ParseJsonNotification(decoded);
    }

    /// <summary>Classic Braintree delivery: base64 of an XML notification document.</summary>
    private static BraintreeWebhookNotification ParseXmlNotification(string xml)
    {
        XDocument document;
        using (var reader = System.Xml.XmlReader.Create(
                   new StringReader(xml),
                   new System.Xml.XmlReaderSettings
                   {
                       DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                       XmlResolver = null,
                       IgnoreComments = true,
                       IgnoreProcessingInstructions = true
                   }))
        {
            document = XDocument.Load(reader);
        }

        var root = document.Root ?? throw new FormatException("Braintree XML notification has no root element.");
        var subject = root.Element("subject");

        var subscription = subject?.Element("subscription");
        var transaction = subject?.Element("transaction");
        var dispute = subject?.Element("dispute");

        return new BraintreeWebhookNotification
        {
            Kind = root.Element("kind")?.Value.Trim() ?? string.Empty,
            TimestampUtc = ParseTimestamp(root.Element("timestamp")?.Value),
            SubscriptionId = subscription?.Element("id")?.Value.Trim(),
            SubscriptionStatus = subscription?.Element("status")?.Value.Trim(),
            TransactionId = transaction?.Element("id")?.Value.Trim(),
            TransactionStatus = transaction?.Element("status")?.Value.Trim(),
            Amount = ParseDecimal(
                transaction?.Element("amount")?.Value
                ?? dispute?.Element("amount")?.Value
                ?? subscription?.Element("price")?.Value),
            CurrencyIsoCode = transaction?.Element("currency-iso-code")?.Value.Trim()
                              ?? dispute?.Element("currency-iso-code")?.Value.Trim(),
            DisputeId = dispute?.Element("id")?.Value.Trim(),
            DisputeStatus = dispute?.Element("status")?.Value.Trim()
        };
    }

    /// <summary>GraphQL-era delivery: base64 of a JSON notification document.</summary>
    private static BraintreeWebhookNotification ParseJsonNotification(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Braintree JSON notification must be an object.");
        }

        string? ReadString(params string[] names)
        {
            foreach (var name in names)
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.Trim();
                    }
                }
            }

            return null;
        }

        BraintreeWebhookNotification FromSubject(JsonElement subject)
        {
            string? SubjectString(params string[] names)
            {
                foreach (var name in names)
                {
                    if (subject.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        var text = value.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text.Trim();
                        }
                    }
                }

                return null;
            }

            _ = subject.TryGetProperty("subscription", out var subscription);
            _ = subject.TryGetProperty("transaction", out var transaction);
            _ = subject.TryGetProperty("dispute", out var dispute);

            return new BraintreeWebhookNotification
            {
                Kind = ReadString("kind", "type") ?? string.Empty,
                TimestampUtc = ParseTimestamp(ReadString("timestamp")),
                SubscriptionId = subscription.ValueKind == JsonValueKind.Object
                    ? SubjectStringOf(subscription, "id")
                    : SubjectString("subscriptionId"),
                SubscriptionStatus = subscription.ValueKind == JsonValueKind.Object
                    ? SubjectStringOf(subscription, "status")
                    : null,
                TransactionId = transaction.ValueKind == JsonValueKind.Object
                    ? SubjectStringOf(transaction, "id")
                    : SubjectString("transactionId"),
                TransactionStatus = transaction.ValueKind == JsonValueKind.Object
                    ? SubjectStringOf(transaction, "status")
                    : null,
                Amount = transaction.ValueKind == JsonValueKind.Object
                    ? SubjectAmountOf(transaction, "amount")
                    : dispute.ValueKind == JsonValueKind.Object
                        ? SubjectAmountOf(dispute, "amount")
                        : null,
                CurrencyIsoCode = transaction.ValueKind == JsonValueKind.Object
                    ? SubjectStringOf(transaction, "currencyIsoCode", "currency_iso_code")
                    : null,
                DisputeId = dispute.ValueKind == JsonValueKind.Object
                    ? SubjectStringOf(dispute, "id")
                    : SubjectString("disputeId"),
                DisputeStatus = dispute.ValueKind == JsonValueKind.Object
                    ? SubjectStringOf(dispute, "status")
                    : null
            };
        }

        if (root.TryGetProperty("subject", out var subjectElement) && subjectElement.ValueKind == JsonValueKind.Object)
        {
            return FromSubject(subjectElement);
        }

        return new BraintreeWebhookNotification
        {
            Kind = ReadString("kind", "type") ?? string.Empty,
            TimestampUtc = ParseTimestamp(ReadString("timestamp"))
        };
    }

    private static string? SubjectStringOf(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }
        }

        return null;
    }

    private static decimal? SubjectAmountOf(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var amount))
                {
                    return amount;
                }

                if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     Base64-decodes the payload, tolerating the URL-safe alphabet and stripped padding
    ///     some Braintree integrations emit for the form field.
    /// </summary>
    internal static string DecodeBase64Flexible(string payload)
    {
        var normalized = payload.Replace('-', '+').Replace('_', '/').TrimEnd('=');
        var padding = (4 - normalized.Length % 4) % 4;
        var bytes = Convert.FromBase64String(normalized.PadRight(normalized.Length + padding, '='));
        return Encoding.UTF8.GetString(bytes);
    }

    private static string Sha1HexDigest(string input)
    {
        var digest = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static bool FixedTimeEqualsUtf8(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static DateTime? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseDecimal(string? value) =>
        !string.IsNullOrWhiteSpace(value) && decimal.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
}
