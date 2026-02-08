using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles the GitHubSignInCommand by delegating to IOAuthService
///     to generate the GitHub OAuth authorization URL.
/// </summary>
public sealed class GitHubSignInCommandHandler(
    IOAuthService oAuthService,
    ILogger<GitHubSignInCommandHandler> logger
) : ICommandHandler<GitHubSignInCommand, GitHubSignInResponse>
{
    public async Task<GitHubSignInResponse> Handle(GitHubSignInCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Initiating GitHub OAuth sign-in with redirect to {RedirectUri}", request.RedirectUri);

        var state = Guid.NewGuid().ToString("N");
        var authUrl = await oAuthService.GetAuthorizationUrlAsync(
            "github",
            request.RedirectUri,
            state
        ).ConfigureAwait(false);

        return new GitHubSignInResponse { AuthUrl = authUrl };
    }
}
