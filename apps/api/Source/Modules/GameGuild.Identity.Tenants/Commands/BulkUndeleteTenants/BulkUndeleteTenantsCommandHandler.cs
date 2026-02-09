using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public sealed class BulkUndeleteTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkUndeleteTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkUndeleteTenantsCommand request, CancellationToken cancellationToken)
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
