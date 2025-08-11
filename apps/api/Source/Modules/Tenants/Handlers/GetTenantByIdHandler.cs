using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for getting a tenant by ID
/// </summary>
public class GetTenantByIdHandler(
  ApplicationDbContext context,
  ILogger<GetTenantByIdHandler> logger
) : IQueryHandler<GetTenantByIdQuery, Common.Result<Tenant?>> {
  public async Task<Common.Result<Tenant?>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken) {
    try {
      var query = context.Resources.OfType<Tenant>()
                         .Where(t => t.Id == request.Id);

      if (!request.IncludeDeleted) query = query.Where(t => t.DeletedAt == null);

      var tenant = await query.FirstOrDefaultAsync(cancellationToken);

      logger.LogInformation("Retrieved tenant {TenantId}: {Found}", request.Id, tenant != null ? "Found" : "Not Found");

      return Result.Success(tenant);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error retrieving tenant {TenantId}", request.Id);

      return Result.Failure<Tenant?>(
        Common.Error.Failure("Tenant.RetrievalFailed", "Failed to retrieve tenant")
      );
    }
  }
}
