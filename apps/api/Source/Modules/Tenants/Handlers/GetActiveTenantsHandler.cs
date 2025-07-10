using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for getting active tenants
/// </summary>
public class GetActiveTenantsHandler(
  ApplicationDbContext context,
  ILogger<GetActiveTenantsHandler> logger
) : IQueryHandler<GetActiveTenantsQuery, Common.Result<IEnumerable<Tenant>>>
{
  public async Task<Common.Result<IEnumerable<Tenant>>> Handle(GetActiveTenantsQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var tenants = await context.Resources.OfType<Tenant>()
                                 .Where(t => t.IsActive && t.DeletedAt == null)
                                 .ToListAsync(cancellationToken);

      logger.LogInformation("Retrieved {Count} active tenants", tenants.Count);
      return Result.Success<IEnumerable<Tenant>>(tenants);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving active tenants");
      return Result.Failure<IEnumerable<Tenant>>(
        Common.Error.Failure("Tenant.RetrievalFailed", "Failed to retrieve active tenants")
      );
    }
  }
}