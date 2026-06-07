namespace GameGuild.Identity.Authentication;

/// <summary>
///     Cryptographic proof for a verifiable credential.
/// </summary>
public abstract class CredentialProof
{
    /// <summary>
    ///     Proof type (e.g., "Ed25519Signature2018").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     When the proof was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    ///     Verification method identifier.
    /// </summary>
    public string VerificationMethod { get; set; } = string.Empty;

    /// <summary>
    ///     Proof purpose (e.g., "assertionMethod").
    /// </summary>
    public string ProofPurpose { get; set; } = string.Empty;

    /// <summary>
    ///     Proof value (signature).
    /// </summary>
    public string ProofValue { get; set; } = string.Empty;
}
