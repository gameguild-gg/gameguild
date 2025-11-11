using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Commands;

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
        var errors = new List<string>();

        foreach (var tenantId in tenantIdsList)
        {
            try
            {
                var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);

                if (tenant == null)
                {
                    errors.Add($"Tenant with ID {tenantId} not found");
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
            catch (Exception ex)
            {
                errors.Add($"Failed to archive tenant {tenantId}: {ex.Message}");
                failed++;
            }
        }

        var Response = new BulkOperationResponse { TotalRequested = totalRequested, SuccessfulOperations = successful, FailedOperations = failed };

        return Response;
    }
}
