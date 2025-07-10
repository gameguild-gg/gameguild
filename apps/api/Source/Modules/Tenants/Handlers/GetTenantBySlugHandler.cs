using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for getting a tenant by slug
/// </summary>
public class GetTenantBySlugHandler(
  ApplicationDbContext context,
  ILogger<GetTenantBySlugHandler> logger
) : IQueryHandler<GetTenantBySlugQuery, Common.Result<Tenant?>>
{
  public async Task<Common.Result<Tenant?>> Handle(GetTenantBySlugQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var query = context.Resources.OfType<Tenant>()
                         .Where(t => t.Slug == request.Slug);

      if (!request.IncludeDeleted)
      {
        query = query.Where(t => t.DeletedAt == null);
      }

      var tenant = await query.FirstOrDefaultAsync(cancellationToken);

      logger.LogInformation("Retrieved tenant by slug '{TenantSlug}': {Found}", request.Slug, tenant != null ? "Found" : "Not Found");
      return Result.Success(tenant);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving tenant by slug '{TenantSlug}'", request.Slug);
      return Result.Failure<Tenant?>(
        Common.Error.Failure("Tenant.RetrievalFailed", "Failed to retrieve tenant by slug")
      );
    }
  }
}