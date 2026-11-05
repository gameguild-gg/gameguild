using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

/// <summary>Removes both certificate pins without changing the account's client secret.</summary>
public sealed record UnbindServiceAccountCertificateCommand(Guid ServiceAccountId) : ICommand<bool>;

public sealed class UnbindServiceAccountCertificateCommandHandler(
    IServiceAccountService serviceAccountService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<UnbindServiceAccountCertificateCommand, bool>
{
    public async Task<bool> Handle(UnbindServiceAccountCertificateCommand command, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.ActorKind != ActorKind.User ||
            actor.SubjectIdAsGuid is not { } subjectId || subjectId == Guid.Empty || !actor.IsTenantAdmin)
        {
            throw new UnauthorizedAccessException("An authenticated administrator is required to remove certificate bindings.");
        }

        var account = await serviceAccountService.GetByIdAsync(command.ServiceAccountId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return false;
        }

        if (!actor.IsSystemAdmin &&
            (!account.TenantId.HasValue || account.TenantId == Guid.Empty || actor.TenantId != account.TenantId))
        {
            throw new UnauthorizedAccessException("Certificate bindings can only be removed by an administrator in the account's tenant.");
        }

        var previousThumbprint = account.CertificateThumbprint;
        var previousSpki = account.CertificateSpkiSha256;
        var previousUpdatedAt = account.UpdatedAt;
        account.UnbindCertificate();
        try
        {
            await serviceAccountService.UpdateAsync(account, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            account.CertificateThumbprint = previousThumbprint;
            account.CertificateSpkiSha256 = previousSpki;
            account.UpdatedAt = previousUpdatedAt;
            throw;
        }

        return true;
    }
}
