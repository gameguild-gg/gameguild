using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public sealed class BulkUndeleteTenantsCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<BulkUndeleteTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkUndeleteTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenantIdsList = request.TenantIds.ToList();
        var totalRequested = tenantIdsList.Count;
        var successful = 0;
        var failed = 0;

        // Issue #1287 T1: authorization is pre-flighted over every resolved target
        // BEFORE the first mutation — a batch containing a tenant the actor cannot
        // manage (no active Owner/Admin membership, not a system administrator) fails
        // closed with 403 and nothing is changed. Missing tenants keep their per-item
        // failure accounting.
        var targets = new List<Tenant>();
        foreach (var tenantId in tenantIdsList)
        {
            var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (tenant == null)
            {
                failed++;
                continue;
            }

            targets.Add(tenant);
        }

        foreach (var tenant in targets)
        {
            await administrationGuard.EnsureCanMutateAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
        }

        foreach (var tenant in targets)
        {
            try
            {
                if (!tenant.IsDeleted)
                {
                    successful++; // Already not deleted, count as success
                    continue;
                }

                tenant.Restore();
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
