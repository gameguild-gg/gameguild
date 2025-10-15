using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for permanently deleting a tenant
/// </summary>
public class HardDeleteTenantHandler(ApplicationDbContext context, ILogger<HardDeleteTenantHandler> logger) : ICommandHandler<HardDeleteTenantCommand, Common.Result<bool>> {
  public async Task<Common.Result<bool>> Handle(HardDeleteTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>()
                                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

      if (tenant == null)
        return Result.Failure<bool>(
          Common.Error.NotFound("Tenant.NotFound", $"Tenant with ID {request.Id} not found")
        );

      context.Resources.Remove(tenant);
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} permanently deleted", tenant.Id);

      return Result.Success(true);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error permanently deleting tenant {TenantId}", request.Id);

      return Result.Failure<bool>(
        Common.Error.Failure("Tenant.HardDeleteFailed", "Failed to permanently delete tenant")
      );
    }
  }
}
