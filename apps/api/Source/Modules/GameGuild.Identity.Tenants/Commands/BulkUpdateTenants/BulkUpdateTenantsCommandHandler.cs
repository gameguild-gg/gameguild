using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public sealed class BulkUpdateTenantsCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<BulkUpdateTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkUpdateTenantsCommand request, CancellationToken cancellationToken)
    {
        var updates = request.Updates.ToList();
        var totalRequested = updates.Count;
        var successful = 0;
        var failed = 0;

        // Issue #1287 T1: bulk verbs mutate arbitrary tenants by id, so authorization
        // is pre-flighted over every resolved target BEFORE the first mutation — a
        // batch containing a tenant the actor cannot manage (no active Owner/Admin
        // membership, not a system administrator) fails closed with 403 and nothing
        // is changed. Missing tenants keep their per-item failure accounting.
        var targets = new List<(Tenant Tenant, BulkUpdateTenantItem Update)>();
        foreach (var update in updates)
        {
            var tenant = await tenantRepository.GetByIdAsync(update.TenantId, cancellationToken).ConfigureAwait(false);
            if (tenant == null)
            {
                failed++;
                continue;
            }

            targets.Add((tenant, update));
        }

        foreach (var (tenant, _) in targets)
        {
            await administrationGuard.EnsureCanMutateAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
        }

        foreach (var (tenant, update) in targets)
        {
            try
            {
                tenant.Update(update.Name, update.Description);
                await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
                successful++;
            }
            catch
            {
                failed++;
            }
        }

        return new BulkOperationResponse
        {
            TotalRequested = totalRequested,
            SuccessfulOperations = successful,
            FailedOperations = failed
        };
    }
}
