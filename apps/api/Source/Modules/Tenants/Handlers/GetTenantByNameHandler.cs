using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Tenants.Queries;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants.Handlers;

/// <summary>
/// Handler for getting a tenant by name
/// </summary>
public class GetTenantByNameHandler(
  ApplicationDbContext context,
  ILogger<GetTenantByNameHandler> logger
) : IQueryHandler<GetTenantByNameQuery, Common.Result<Tenant?>>
{
  public async Task<Common.Result<Tenant?>> Handle(GetTenantByNameQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var query = context.Resources.OfType<Tenant>()
                         .Where(t => t.Name == request.Name);

      if (!request.IncludeDeleted)
      {
        query = query.Where(t => t.DeletedAt == null);
      }

      var tenant = await query.FirstOrDefaultAsync(cancellationToken);

      logger.LogInformation("Retrieved tenant by name '{TenantName}': {Found}", request.Name, tenant != null ? "Found" : "Not Found");
      return Result.Success(tenant);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving tenant by name '{TenantName}'", request.Name);
      return Result.Failure<Tenant?>(
        Common.Error.Failure("Tenant.RetrievalFailed", "Failed to retrieve tenant by name")
      );
    }
  }
}