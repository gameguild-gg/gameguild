using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for bulk archiving of tenants
/// </summary>
public class BulkArchiveTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkArchiveTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkArchiveTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenantIdsList = request.TenantIds.ToList();
        var totalRequested = tenantIdsList.Count;
        var successful = 0;
        var failed = 0;

        foreach (var tenantId in tenantIdsList)
        {
            try
            {
                var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);

                if (tenant == null)
                {
                    failed++;

                    continue;
                }

                if (tenant.IsArchived)
                {
                    // Skip already archived tenants - count as successful
                    successful++;

                    continue;
                }

                tenant.Archive();
                await tenantRepository.UpdateAsync(tenant, cancellationToken);

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
