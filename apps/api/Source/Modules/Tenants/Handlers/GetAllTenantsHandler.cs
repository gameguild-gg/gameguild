using GameGuild.CQRS;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for getting all tenants
/// </summary>
public class GetAllTenantsHandler(ApplicationDbContext context, ILogger<GetAllTenantsHandler> logger) : IQueryHandler<GetAllTenantsQuery, Result<IEnumerable<Tenant>>> {
  public async Task<Result<IEnumerable<Tenant>>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken) {
    try {
      var query = context.Resources.OfType<Tenant>();

      if (!request.IncludeDeleted) query = query.Where(t => t.DeletedAt == null);

      var tenants = await query.ToListAsync(cancellationToken);

      logger.LogInformation("Retrieved {Count} tenants", tenants.Count);

      return Result.Success<IEnumerable<Tenant>>(tenants);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error retrieving tenants");

      return Result.Failure<IEnumerable<Tenant>>(Error.Failure("Tenant.RetrievalFailed", "Failed to retrieve tenants"));
    }
  }
}
