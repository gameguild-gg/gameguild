using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for bulk deletion of tenants
/// </summary>
public sealed class BulkDeleteTenantsCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<BulkDeleteTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkDeleteTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenantIdsList = request.TenantIds.ToList();
        var totalRequested = tenantIdsList.Count;
        var successful = 0;
        var failed = 0;

        // Issue #1287 T1: authorization is pre-flighted over every resolved target
        // BEFORE the first mutation. Hard deletes are the destructive tier (active
        // Owner membership of each tenant or system administration); soft deletes are
        // the management tier (active Owner/Admin membership or system administration).
        // A batch the actor is not entitled to fails closed with 403 and nothing is
        // changed. Missing tenants keep their per-item failure accounting.
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
            if (request.HardDelete)
            {
                await administrationGuard.EnsureCanPurgeAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await administrationGuard.EnsureCanMutateAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var tenant in targets)
        {
            try
            {
                if (request.HardDelete) { await tenantRepository.DeleteAsync(tenant, cancellationToken).ConfigureAwait(false); }
                else
                {
                    tenant.SoftDelete();
                    await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
                }

                successful++;
            }
            catch
            {
                failed++;
            }
        }

        var response = new BulkOperationResponse { TotalRequested = totalRequested, SuccessfulOperations = successful, FailedOperations = failed };

        return response;
    }
}
