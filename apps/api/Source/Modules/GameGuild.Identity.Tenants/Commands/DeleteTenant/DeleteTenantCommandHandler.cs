using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for deleting tenant command
/// </summary>
public sealed class DeleteTenantCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard,
    GameGuild.CQRS.IPublisher? publisher = null) : ICommandHandler<DeleteTenantCommand>
{
    public async Task<Unit> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Issue #1287 T1: hard deletion is the destructive tier — only the tenant's
        // active Owner membership (or a system administrator) may purge it.
        await administrationGuard.EnsureCanPurgeAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            throw new EntityNotFoundException(nameof(Tenant), request.TenantId);
        }

        // Issue #1287 T1a: consult the entity's deletion contract — the delete
        // precondition (archived first, no active members) is enforced, not assumed.
        var deletion = tenant.ValidateForDeletion();
        if (!deletion.IsSuccess)
        {
            throw new BusinessRuleViolationException(
                "Tenant.ValidateForDeletion",
                deletion.Error ?? "Tenant cannot be deleted.",
                new { tenant.Id });
        }

        await tenantRepository.DeleteAsync(tenant, cancellationToken).ConfigureAwait(false);

        // Issue #1184 G4: TenantDeletedEvent was declared but never raised; tenant deletion
        // is its producing site (cleanup consumers react to the removed tenant).
        if (publisher is not null)
        {
            await publisher.Publish(new TenantDeletedEvent(tenant.Id, tenant.Name), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
