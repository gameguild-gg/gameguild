using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for restoring a tenant
/// </summary>
public sealed class RestoreTenantCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<RestoreTenantCommand, RestoreTenantResponse>
{
    public async Task<RestoreTenantResponse> Handle(RestoreTenantCommand request, CancellationToken cancellationToken)
    {
        // Issue #1287 T1: restoring is a lifecycle mutation gated to active
        // Owner/Admin membership of the target tenant (or system administration).
        await administrationGuard.EnsureCanMutateAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new RestoreTenantResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found", TenantId = request.TenantId }; }

        if (!tenant.IsArchived) { return new RestoreTenantResponse { Success = true, Message = "Tenant is not archived", TenantId = request.TenantId }; }

        tenant.Unarchive();
        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return new RestoreTenantResponse { Success = true, Message = "Tenant restored successfully", TenantId = request.TenantId };
    }
}
