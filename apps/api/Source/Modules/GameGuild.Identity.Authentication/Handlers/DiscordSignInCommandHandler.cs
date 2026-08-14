using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles the DiscordSignInCommand by delegating to IOAuthService
///     to generate the Discord OAuth authorization URL.
/// </summary>
public sealed class DiscordSignInCommandHandler(
    IOAuthService oAuthService,
    ILogger<DiscordSignInCommandHandler> logger
) : ICommandHandler<DiscordSignInCommand, DiscordSignInResponse>
{
    public async Task<DiscordSignInResponse> Handle(DiscordSignInCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Initiating Discord OAuth sign-in with redirect to {RedirectUri}", request.RedirectUri);

        var state = Guid.NewGuid().ToString("N");
        var authUrl = await oAuthService.GetAuthorizationUrlAsync(
            "discord",
            request.RedirectUri,
            state
        ).ConfigureAwait(false);

        return new DiscordSignInResponse { AuthUrl = authUrl, State = state };
    }
}
