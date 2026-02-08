namespace GameGuild.Identity.Authentication;

/// <summary>
/// Service interface for Web3 authentication: challenge generation and signature verification
/// </summary>
public interface IWeb3AuthService
{
    Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> VerifyWeb3SignatureAsync(Web3VerificationRequest request, CancellationToken cancellationToken = default);
}
