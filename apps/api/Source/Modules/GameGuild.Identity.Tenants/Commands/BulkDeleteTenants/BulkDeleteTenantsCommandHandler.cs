using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for bulk deletion of tenants
/// </summary>
public sealed class BulkDeleteTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkDeleteTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkDeleteTenantsCommand request, CancellationToken cancellationToken)
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
