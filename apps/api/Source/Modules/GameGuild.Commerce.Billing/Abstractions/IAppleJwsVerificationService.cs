namespace GameGuild.Commerce.Billing;

/// <summary>
///     Service for decoding and verifying Apple JWS (JSON Web Signature) payloads,
///     including signed transactions and App Store Server Notifications.
/// </summary>
public interface IAppleJwsVerificationService
{
    /// <summary>
    ///     Decodes and verifies a signed transaction JWS from Apple using X.509 certificate chain validation.
    /// </summary>
    /// <param name="signedTransaction">The JWS-encoded signed transaction from the App Store Server API</param>
    /// <returns>Decoded transaction info, or null if verification fails</returns>
    AppleTransactionInfo? DecodeSignedTransaction(string signedTransaction);

    /// <summary>
    ///     Decodes and verifies a signed notification JWS from Apple using X.509 certificate chain validation.
    /// </summary>
    /// <param name="signedPayload">The JWS-encoded signed notification payload</param>
    /// <returns>Decoded notification payload, or null if verification fails</returns>
    AppleNotificationPayload? DecodeSignedNotification(string signedPayload);
}
