using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public sealed class BulkUpdateTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkUpdateTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkUpdateTenantsCommand request, CancellationToken cancellationToken)
    {
        var updates = request.Updates.ToList();
        var totalRequested = updates.Count;
        var successful = 0;
        var failed = 0;

        foreach (var update in updates)
        {
            try
            {
                var tenant = await tenantRepository.GetByIdAsync(update.TenantId, cancellationToken).ConfigureAwait(false);
                if (tenant == null)
                {
                    failed++;
                    continue;
                }

                tenant.Update(update.Name, update.Description);
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
