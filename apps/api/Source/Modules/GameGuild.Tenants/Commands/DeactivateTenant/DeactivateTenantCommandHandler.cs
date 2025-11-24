using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Handler for deactivating tenant command
/// </summary>
public class DeactivateTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<DeactivateTenantCommand>
{
    public async Task<Unit> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { throw new InvalidOperationException($"Tenant with ID {request.TenantId} not found."); }

        tenant.Deactivate();
        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
