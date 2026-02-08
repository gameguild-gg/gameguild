using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for restoring a tenant
/// </summary>
public class RestoreTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<RestoreTenantCommand, RestoreTenantResponse>
{
    public async Task<RestoreTenantResponse> Handle(RestoreTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new RestoreTenantResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found", TenantId = request.TenantId }; }

        if (!tenant.IsArchived) { return new RestoreTenantResponse { Success = true, Message = "Tenant is not archived", TenantId = request.TenantId }; }

        tenant.Unarchive();
        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return new RestoreTenantResponse { Success = true, Message = "Tenant restored successfully", TenantId = request.TenantId };
    }
}
