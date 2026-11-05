using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for bulk deactivation of tenants
/// </summary>
public sealed class BulkDeactivateTenantsCommandHandler(
    ITenantRepository tenantRepository,
    ITenantAdministrationGuard administrationGuard) : ICommandHandler<BulkDeactivateTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkDeactivateTenantsCommand request, CancellationToken cancellationToken)
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
                if (!tenant.IsActive)
                {
                    // Skip already inactive tenants - count as successful
                    successful++;

                    continue;
                }

                tenant.Deactivate();
                await tenantRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

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
