using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for bulk activation of tenants
/// </summary>
public sealed class BulkActivateTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkActivateTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkActivateTenantsCommand request, CancellationToken cancellationToken)
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

                if (tenant.IsActive)
                {
                    // Skip already active tenants - count as successful
                    successful++;

                    continue;
                }

                tenant.Activate();
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
