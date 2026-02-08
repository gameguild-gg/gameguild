using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public class BulkCreateTenantsCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<BulkCreateTenantsCommand, BulkOperationResponse>
{
    public async Task<BulkOperationResponse> Handle(BulkCreateTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenantItems = request.Tenants.ToList();
        var totalRequested = tenantItems.Count;
        var successful = 0;
        var failed = 0;
        var errors = new List<BulkOperationError>();

        foreach (var item in tenantItems)
        {
            try
            {
                var isSlugUnique = await tenantRepository.IsSlugUniqueAsync(item.Slug, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!isSlugUnique)
                {
                    failed++;
                    continue;
                }

                var tenant = new Tenant
                {
                    Name = item.Name,
                    Slug = item.Slug,
                    AdminEmail = item.AdminEmail,
                    Description = item.Description,
                    IsActive = true
                };

                await tenantRepository.CreateAsync(tenant, cancellationToken).ConfigureAwait(false);
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
            FailedOperations = failed,
            Errors = errors
        };
    }
}
