using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

public sealed class GetExternalLoginsQueryHandler(IExternalLoginRepository externalLoginRepository)
    : IQueryHandler<GetExternalLoginsQuery, List<ExternalLoginDto>>
{
    public async Task<List<ExternalLoginDto>> Handle(GetExternalLoginsQuery request, CancellationToken cancellationToken)
    {
        var logins = await externalLoginRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        return logins
            .Select(l => new ExternalLoginDto { Provider = l.Provider, CreatedAt = l.CreatedAt })
            .ToList();
    }
}

public sealed class LinkGoogleAccountCommandHandler(
    IGoogleIdTokenVerifier googleIdTokenVerifier,
    IExternalLoginRepository externalLoginRepository
) : ICommandHandler<LinkGoogleAccountCommand>
{
    public async Task<Unit> Handle(LinkGoogleAccountCommand request, CancellationToken cancellationToken)
    {
        var verified = await googleIdTokenVerifier.VerifyAsync(request.IdToken, cancellationToken).ConfigureAwait(false);

        await ExternalLoginLinking.LinkAsync(externalLoginRepository, "google", verified.Sub, request.UserId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

public sealed class DiscordLinkAuthorizeCommandHandler(
    IOAuthService oAuthService,
    ILogger<DiscordLinkAuthorizeCommandHandler> logger
) : ICommandHandler<DiscordLinkAuthorizeCommand, DiscordLinkAuthorizeResponse>
{
    public async Task<DiscordLinkAuthorizeResponse> Handle(DiscordLinkAuthorizeCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Initiating Discord OAuth link flow with redirect to {RedirectUri}", request.RedirectUri);

        var state = Guid.NewGuid().ToString("N");
        var authUrl = await oAuthService.GetAuthorizationUrlAsync(
            "discord",
            request.RedirectUri,
            state
        ).ConfigureAwait(false);

        return new DiscordLinkAuthorizeResponse { AuthUrl = authUrl, State = state };
    }
}

public sealed class LinkDiscordAccountCommandHandler(
    IOAuthService oAuthService,
    IExternalLoginRepository externalLoginRepository
) : ICommandHandler<LinkDiscordAccountCommand>
{
    public async Task<Unit> Handle(LinkDiscordAccountCommand request, CancellationToken cancellationToken)
    {
        var profile = await oAuthService.HandleCallbackAsync("discord", request.Code, request.State, request.RedirectUri).ConfigureAwait(false);

        await ExternalLoginLinking.LinkAsync(externalLoginRepository, "discord", profile.ProviderId, request.UserId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

public sealed class UnlinkExternalLoginCommandHandler(
    IExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository
) : ICommandHandler<UnlinkExternalLoginCommand>
{
    public async Task<Unit> Handle(UnlinkExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var logins = await externalLoginRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (logins.All(l => l.Provider != request.Provider))
        {
            throw new ExternalLoginNotFoundException($"No {request.Provider} login linked to this account");
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (user?.PasswordHash is null && logins.Count == 1)
        {
            throw new LastSignInMethodException("Cannot remove the last sign-in method");
        }

        await externalLoginRepository.DeleteAsync(request.Provider, request.UserId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

/// <summary>
///     Shared three-way link rule for authenticated account linking (plan B1):
///     no existing row → insert; same user → idempotent no-op; different user → 409 conflict.
///     The insert uses insert-only AddAsync — UpsertAsync is FORBIDDEN here because its internal
///     read-then-update path silently reassigns row ownership when a concurrent request committed
///     the same (Provider, ProviderKey) between our pre-check and the write.
/// </summary>
internal static class ExternalLoginLinking
{
    public static async Task LinkAsync(
        IExternalLoginRepository externalLoginRepository,
        string provider,
        string providerKey,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var existing = await externalLoginRepository.GetByProviderKeyAsync(provider, providerKey, cancellationToken).ConfigureAwait(false);

        if (existing == null)
        {
            try
            {
                await externalLoginRepository.AddAsync(
                    new ExternalLogin { UserId = userId, Provider = provider, ProviderKey = providerKey },
                    cancellationToken).ConfigureAwait(false);

                return;
            }
            catch (DbUpdateException)
            {
                // Unique-index race: a concurrent request inserted the same (Provider, ProviderKey).
                // Refetch and apply the same three-way rule below.
                existing = await externalLoginRepository.GetByProviderKeyAsync(provider, providerKey, cancellationToken).ConfigureAwait(false);

                if (existing == null) { throw; }
            }
        }

        if (existing.UserId == userId) { return; }

        throw new ExternalLoginConflictException("Social account already linked to another user");
    }
}
