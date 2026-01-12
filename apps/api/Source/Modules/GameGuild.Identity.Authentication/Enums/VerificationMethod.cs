namespace GameGuild.Identity.Authentication;

/// <summary>
///     Methods available for identity verification.
/// </summary>
public enum VerificationMethod
{
    /// <summary>
    ///     Email verification via link or code.
    /// </summary>
    Email,

    /// <summary>
    ///     Phone verification via SMS code.
    /// </summary>
    Sms,

    /// <summary>
    ///     Phone verification via voice call.
    /// </summary>
    VoiceCall,

    /// <summary>
    ///     Government-issued ID document verification.
    /// </summary>
    GovernmentId,

    /// <summary>
    ///     Biometric verification (fingerprint, face, etc.).
    /// </summary>
    Biometric,

    /// <summary>
    ///     Blockchain/wallet signature verification.
    /// </summary>
    Web3Signature,

    /// <summary>
    ///     Manual review by administrator.
    /// </summary>
    ManualReview,

    /// <summary>
    ///     Third-party KYC service verification.
    /// </summary>
    ThirdPartyKyc
}
