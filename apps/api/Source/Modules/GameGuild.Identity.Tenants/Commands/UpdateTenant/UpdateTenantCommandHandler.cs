using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for updating tenant command
/// </summary>
public sealed class UpdateTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<UpdateTenantCommand>
{
    public async Task<Unit> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { throw new InvalidOperationException($"Tenant with ID {request.TenantId} not found."); }

        // Update tenant information
        if (!string.IsNullOrEmpty(request.Name)) { tenant.Update(request.Name, request.Description); }

        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
