using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Handler for archiving a tenant
/// </summary>
public class ArchiveTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<ArchiveTenantCommand, ArchiveTenantResponse>
{
    public async Task<ArchiveTenantResponse> Handle(ArchiveTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);

        if (tenant == null) { return new ArchiveTenantResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found", TenantId = request.TenantId }; }

        if (tenant.IsArchived) { return new ArchiveTenantResponse { Success = true, Message = "Tenant is already archived", TenantId = request.TenantId }; }

        tenant.Archive(request.Reason);
        await tenantRepository.UpdateAsync(tenant, cancellationToken);

        return new ArchiveTenantResponse { Success = true, Message = "Tenant archived successfully", TenantId = request.TenantId };
    }
}
