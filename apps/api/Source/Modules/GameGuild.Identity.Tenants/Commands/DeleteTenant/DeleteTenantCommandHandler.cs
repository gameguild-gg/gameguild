using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for deleting tenant command
/// </summary>
public sealed class DeleteTenantCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<DeleteTenantCommand>
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

        return Unit.Value;
    }
}
