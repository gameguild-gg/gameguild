using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for permanent bulk purge of tenants (hard delete)
/// </summary>
public sealed class BulkPurgeTenantsCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<BulkPurgeTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkPurgeTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenantIdsList = request.TenantIds.ToList();
        var totalRequested = tenantIdsList.Count;
        var successful = 0;
        var failed = 0;

        // Issue #1287 T1: hard purge is the destructive tier — authorization is
        // pre-flighted over every resolved target BEFORE the first deletion, requiring
        // the active Owner membership of EACH tenant (or system administration). A
        // batch the actor is not entitled to fails closed with 403 and nothing is
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
            await administrationGuard.EnsureCanPurgeAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
        }

        foreach (var tenant in targets)
        {
            try
            {
                await tenantRepository.DeleteAsync(tenant, cancellationToken).ConfigureAwait(false);
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
