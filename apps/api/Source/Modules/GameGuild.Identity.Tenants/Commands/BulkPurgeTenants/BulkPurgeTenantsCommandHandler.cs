using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for permanent bulk purge of tenants (hard delete)
/// </summary>
public class BulkPurgeTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkPurgeTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkPurgeTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenantIdsList = request.TenantIds.ToList();
        var totalRequested = tenantIdsList.Count;
        var successful = 0;
        var failed = 0;

        foreach (var tenantId in tenantIdsList)
        {
            try
            {
                var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
                if (tenant == null)
                {
                    failed++;
                    continue;
                }

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
