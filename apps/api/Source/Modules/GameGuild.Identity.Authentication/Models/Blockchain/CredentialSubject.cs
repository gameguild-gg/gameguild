namespace GameGuild.Identity.Authentication;

/// <summary>
///     Subject of a verifiable credential.
/// </summary>
public class CredentialSubject
{
    /// <summary>
    ///     Decentralized Identifier (DID) of the subject.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Claims about the subject.
    /// </summary>
    public Dictionary<string, object> Claims { get; set; } = new Dictionary<string, object>();
}
