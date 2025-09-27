namespace GameGuild.Modules.Authentication;


/// <summary>
/// DTO for Web3 sign-in requests
/// </summary>
public class Web3SignInRequest
{
    public string WalletAddress { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
