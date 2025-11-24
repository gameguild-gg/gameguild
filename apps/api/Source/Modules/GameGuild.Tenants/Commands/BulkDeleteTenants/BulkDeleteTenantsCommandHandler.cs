using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Handler for bulk deletion of tenants
/// </summary>
public class BulkDeleteTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkDeleteTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkDeleteTenantsCommand request, CancellationToken cancellationToken)
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

                if (request.HardDelete) { await tenantRepository.DeleteAsync(tenant, cancellationToken); }
                else
                {
                    tenant.SoftDelete();
                    await tenantRepository.UpdateAsync(tenant, cancellationToken);
                }

                successful++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to delete tenant {tenantId}: {ex.Message}");
                failed++;
            }
        }

        var Response = new BulkOperationResponse { TotalRequested = totalRequested, SuccessfulOperations = successful, FailedOperations = failed };

        return Response;
    }
}
