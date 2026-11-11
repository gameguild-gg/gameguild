using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for deactivating tenant command
/// </summary>
public sealed class DeactivateTenantCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<DeactivateTenantCommand>
{
    public async Task<Unit> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Issue #1287 T1: lifecycle transitions require an active Owner/Admin
        // membership of the target tenant (or system administration).
        await administrationGuard.EnsureCanMutateAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { throw new InvalidOperationException($"Tenant with ID {request.TenantId} not found."); }

        tenant.Deactivate();
        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
