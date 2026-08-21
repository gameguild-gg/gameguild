using Amazon.SimpleNotificationService.Util;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Result of validating a raw SNS webhook request body: either a cryptographically
/// trusted envelope, or a rejection whose reason maps to the webhook's HTTP status
/// (Malformed → 400, Signature → 401, topic rejections → 403).
/// </summary>
public abstract record SnsVerificationResult
{
    private SnsVerificationResult() { }

    /// <summary>Envelope whose signature was verified against an AWS SNS signing certificate.</summary>
    public sealed record Valid(Message Envelope) : SnsVerificationResult;

    /// <summary>Rejected envelope. Never carries attacker-controlled content.</summary>
    public sealed record Invalid(SnsRejectionReason Rejection, string Reason) : SnsVerificationResult;
}

/// <summary>Why an SNS envelope was rejected.</summary>
public enum SnsRejectionReason
{
    /// <summary>Body is not parseable JSON / not a complete SNS envelope → 400.</summary>
    Malformed,

    /// <summary>Envelope parses but cannot be cryptographically trusted (bad signature, hostile signing cert URL) → 401.</summary>
    Signature,

    /// <summary>Envelope topic does not match the configured EmailDelivery:Events:TopicArn → 403.</summary>
    TopicMismatch,

    /// <summary>No topic ARN configured while running in Production (required there) → 403.</summary>
    TopicNotConfigured
}

/// <summary>
/// Validates raw SNS HTTPS POST bodies for the public email-events webhook: signing certificate
/// host pinning, topic allow-listing, and AWS SDK signature verification — all exception-safe,
/// so hostile input always yields a typed rejection instead of an unhandled 500.
/// </summary>
public interface ISnsMessageVerifier
{
    /// <summary>
    /// Validates the raw request body. Never throws — any parsing or signature failure
    /// (including <c>AmazonClientException</c> from the SDK) is mapped to
    /// <see cref="SnsVerificationResult.Invalid"/>.
    /// </summary>
    SnsVerificationResult ValidateRequest(string body);
}
