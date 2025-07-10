using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Tenants.Queries;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants.Handlers;

/// <summary>
/// Handler for getting deleted tenants
/// </summary>
public class GetDeletedTenantsHandler(
  ApplicationDbContext context,
  ILogger<GetDeletedTenantsHandler> logger
) : IQueryHandler<GetDeletedTenantsQuery, Common.Result<IEnumerable<Tenant>>>
{
  public async Task<Common.Result<IEnumerable<Tenant>>> Handle(GetDeletedTenantsQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var tenants = await context.Resources.OfType<Tenant>()
                                 .Where(t => t.DeletedAt != null)
                                 .ToListAsync(cancellationToken);

      logger.LogInformation("Retrieved {Count} deleted tenants", tenants.Count);
      return Result.Success<IEnumerable<Tenant>>(tenants);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving deleted tenants");
      return Result.Failure<IEnumerable<Tenant>>(
        Common.Error.Failure("Tenant.RetrievalFailed", "Failed to retrieve deleted tenants")
      );
    }
  }
}