using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for activating tenant command
/// </summary>
public class ActivateTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<ActivateTenantCommand, ActivateTenantResponse>
{
    public async Task<ActivateTenantResponse> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new ActivateTenantResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found", TenantId = request.TenantId }; }

        if (tenant.IsActive) { return new ActivateTenantResponse { Success = true, Message = "Tenant is already active", TenantId = request.TenantId }; }

        tenant.Activate();
        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return new ActivateTenantResponse { Success = true, Message = "Tenant activated successfully", TenantId = request.TenantId };
    }
}
