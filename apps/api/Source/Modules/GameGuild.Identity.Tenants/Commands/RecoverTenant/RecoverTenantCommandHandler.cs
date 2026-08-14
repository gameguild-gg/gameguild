using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for recovering an archived tenant.
/// </summary>
public sealed class RecoverTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<RecoverTenantCommand, RecoverTenantResponse>
{
    public async Task<RecoverTenantResponse> Handle(RecoverTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new RecoverTenantResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found", TenantId = request.TenantId }; }

        if (!tenant.IsArchived) { return new RecoverTenantResponse { Success = true, Message = "Tenant is not archived", TenantId = request.TenantId }; }

        tenant.Unarchive();
        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return new RecoverTenantResponse { Success = true, Message = "Tenant recovered successfully", TenantId = request.TenantId };
    }
}