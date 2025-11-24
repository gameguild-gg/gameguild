using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Handler for bulk deactivation of tenants
/// </summary>
public class BulkDeactivateTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkDeactivateTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkDeactivateTenantsCommand request, CancellationToken cancellationToken)
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

                if (!tenant.IsActive)
                {
                    // Skip already inactive tenants - count as successful
                    successful++;

                    continue;
                }

                tenant.Deactivate();
                await tenantRepository.UpdateAsync(tenant, cancellationToken);

                successful++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to deactivate tenant {tenantId}: {ex.Message}");
                failed++;
            }
        }

        var Response = new BulkOperationResponse { TotalRequested = totalRequested, SuccessfulOperations = successful, FailedOperations = failed };

        return Response;
    }
}
