namespace GameGuild.Identity.Authentication;

/// <summary>
///     Web3 credential data
/// </summary>
public class Web3CredentialData : ICredentialData
{
    public string WalletAddress { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get => "web3"; }
}
