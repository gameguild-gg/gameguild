using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for archiving a tenant
/// </summary>
public sealed class ArchiveTenantCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<ArchiveTenantCommand, ArchiveTenantResponse>
{
    public async Task<ArchiveTenantResponse> Handle(ArchiveTenantCommand request, CancellationToken cancellationToken)
    {
        // Issue #1287 T1: archiving (soft delete, reached from both the :archive verb
        // and DELETE /tenants/{id}) requires an active Owner/Admin membership of the
        // target tenant (or system administration).
        await administrationGuard.EnsureCanMutateAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new ArchiveTenantResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found", TenantId = request.TenantId }; }

        if (tenant.IsArchived) { return new ArchiveTenantResponse { Success = true, Message = "Tenant is already archived", TenantId = request.TenantId }; }

        // Issue #1287 T1a: consult the entity's archive contract (currently allows
        // archiving with active members by design; the validator is the contract
        // surface for any future restriction).
        var archive = tenant.ValidateForArchive();
        if (!archive.IsSuccess)
        {
            throw new BusinessRuleViolationException(
                "Tenant.ValidateForArchive",
                archive.Error ?? "Tenant cannot be archived.",
                new { tenant.Id });
        }

        tenant.Archive(request.Reason);
        await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return new ArchiveTenantResponse { Success = true, Message = "Tenant archived successfully", TenantId = request.TenantId };
    }
}
