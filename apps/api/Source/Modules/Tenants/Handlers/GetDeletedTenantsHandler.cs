using GameGuild.CQRS;
using GameGuild.Database;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for getting all deleted tenants
/// </summary>
public class GetDeletedTenantsHandler(ApplicationDbContext context, ILogger<GetDeletedTenantsHandler> logger) : IQueryHandler<GetDeletedTenantsQuery, Result<IEnumerable<Tenant>>>
{
    public async Task<Result<IEnumerable<Tenant>>> Handle(GetDeletedTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Tenant> tenants = await context.Tenants
                .OfType<Tenant>()
                .Where(t => t.DeletedAt != null)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} deleted tenants", tenants.Count());

            return Result.Success(tenants);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deleted tenants");

            return Result.Failure<IEnumerable<Tenant>>(Error.Failure("Tenant.RetrievalFailed", "Failed to retrieve deleted tenants"));
        }
    }
}
