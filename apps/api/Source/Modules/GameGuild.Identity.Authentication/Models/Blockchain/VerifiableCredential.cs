namespace GameGuild.Identity.Authentication;

/// <summary>
///     W3C Verifiable Credential for decentralized identity.
/// </summary>
public abstract class VerifiableCredential
{
    /// <summary>
    ///     JSON-LD context.
    /// </summary>
    public List<string> Context { get; set; } = ["https://www.w3.org/2018/credentials/v1"];

    /// <summary>
    ///     Credential type.
    /// </summary>
    public List<string> Type { get; set; } = ["VerifiableCredential"];

    /// <summary>
    ///     Credential issuer (DID or URL).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    ///     When the credential was issued.
    /// </summary>
    public DateTime IssuanceDate { get; set; }

    /// <summary>
    ///     When the credential expires (optional).
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    ///     Credential subject (who the credential is about).
    /// </summary>
    public CredentialSubject CredentialSubject { get; set; } = new CredentialSubject();

    /// <summary>
    ///     Cryptographic proof.
    /// </summary>
    public CredentialProof? Proof { get; set; }

    /// <summary>
    ///     Gets whether the credential is still valid.
    /// </summary>
    public bool IsValid
    {
        get => !ExpirationDate.HasValue || SystemClock.UtcNow < ExpirationDate.Value;
    }
}
