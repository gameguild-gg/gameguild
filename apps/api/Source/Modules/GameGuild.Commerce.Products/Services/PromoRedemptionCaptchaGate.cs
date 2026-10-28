using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Products;

/// <summary>Decision of the promo redemption CAPTCHA gate.</summary>
public sealed record PromoCaptchaGateDecision(
    bool Allowed,
    string? Reason = null,
    PromoCodeValidationOutcome Outcome = PromoCodeValidationOutcome.Valid)
{
    /// <summary>The gate is satisfied (disabled, trusted context, or verified token).</summary>
    public static readonly PromoCaptchaGateDecision Allow = new(true);

    /// <summary>The gate rejects the redemption — fail closed.</summary>
    public static PromoCaptchaGateDecision Deny(string reason, PromoCodeValidationOutcome outcome) => new(false, reason, outcome);
}

/// <summary>
/// CAPTCHA gate for promo code redemption from untrusted contexts (issue #418
/// anti-farming). Config-flagged via <see cref="PromoRedemptionCaptchaOptions"/>:
/// when enabled, untrusted (anonymous) redemption contexts must present a CAPTCHA
/// token; the gate fails closed on a missing token, an unconfigured verification
/// secret, or a token that does not verify. Trusted contexts (authenticated
/// users) pass without a challenge.
/// </summary>
public interface IPromoRedemptionCaptchaGate
{
    /// <summary>
    /// Evaluates the gate for one redemption attempt. Pure decision logic: no
    /// challenge issuance (the web side owns challenge rendering and later consumes
    /// this gate through the API surface).
    /// </summary>
    /// <param name="isTrustedContext">Whether the caller is authenticated (trusted).</param>
    /// <param name="captchaToken">The CAPTCHA token supplied by the caller, if any.</param>
    PromoCaptchaGateDecision Check(bool isTrustedContext, string? captchaToken);
}

/// <summary>
/// Default gate. Tokens are self-issued HMAC-SHA256 signatures over the challenge
/// payload, verified against <see cref="PromoRedemptionCaptchaOptions.VerificationSecret"/>.
/// With no secret configured the gate denies every untrusted redemption when
/// enabled — fail closed, never fail open.
/// </summary>
public sealed class PromoRedemptionCaptchaGate(
    IOptions<PromoRedemptionCaptchaOptions> options) : IPromoRedemptionCaptchaGate
{
    /// <inheritdoc />
    public PromoCaptchaGateDecision Check(bool isTrustedContext, string? captchaToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return PromoCaptchaGateDecision.Allow;
        }

        if (isTrustedContext)
        {
            return PromoCaptchaGateDecision.Allow;
        }

        if (string.IsNullOrWhiteSpace(settings.VerificationSecret))
        {
            // Enabled without a verifier: fail closed. Untrusted redemption is
            // impossible to verify, so it is rejected outright.
            return PromoCaptchaGateDecision.Deny(
                "CAPTCHA verification is required for this redemption but is not configured; redemption rejected",
                PromoCodeValidationOutcome.CaptchaRequired);
        }

        if (string.IsNullOrWhiteSpace(captchaToken))
        {
            return PromoCaptchaGateDecision.Deny(
                "A CAPTCHA token is required to redeem promo codes from this context",
                PromoCodeValidationOutcome.CaptchaRequired);
        }

        if (!VerifyToken(captchaToken.Trim(), settings.VerificationSecret))
        {
            return PromoCaptchaGateDecision.Deny(
                "The supplied CAPTCHA token is invalid or expired",
                PromoCodeValidationOutcome.CaptchaRequired);
        }

        return PromoCaptchaGateDecision.Allow;
    }

    /// <summary>
    /// Issues a token for the supplied challenge payload — used by tests and by the
    /// web-side challenge endpoint when it lands. <c>payload|timestamp|signature</c>,
    /// Base64, valid for 10 minutes.
    /// </summary>
    public static string IssueToken(string payload, string secret, DateTime? utcNow = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var now = (utcNow ?? SystemClock.UtcNow).ToUniversalTime();
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var body = $"{payload}|{timestamp}";
        var signature = Convert.ToBase64String(HMACSign(body, secret));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{body}|{signature}"));
    }

    private static bool VerifyToken(string token, string secret)
    {
        string body;
        string signature;
        long timestamp;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split('|');
            if (parts.Length != 3)
            {
                return false;
            }

            body = $"{parts[0]}|{parts[1]}";
            timestamp = long.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            signature = parts[2];
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }

        var expected = Convert.ToBase64String(HMACSign(body, secret));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expected)))
        {
            return false;
        }

        // Challenge freshness: 10-minute validity window.
        var nowSeconds = new DateTimeOffset(SystemClock.UtcNow.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeSeconds();
        return timestamp >= nowSeconds - 600 && timestamp <= nowSeconds + 600;
    }

    private static byte[] HMACSign(string body, string secret)
        => HMACSHA256.HashData(Encoding.UTF8.GetBytes(body), Encoding.UTF8.GetBytes(secret));
}
