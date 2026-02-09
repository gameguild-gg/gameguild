using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for bulk deactivation of tenants
/// </summary>
public sealed class BulkDeactivateTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkDeactivateTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkDeactivateTenantsCommand request, CancellationToken cancellationToken)
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
