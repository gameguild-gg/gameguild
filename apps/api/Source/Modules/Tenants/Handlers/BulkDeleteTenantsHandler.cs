using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for bulk deleting tenants
/// </summary>
public class BulkDeleteTenantsHandler(
  ApplicationDbContext context,
  ILogger<BulkDeleteTenantsHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<BulkDeleteTenantsCommand, Common.Result<int>> {
  public async Task<Common.Result<int>> Handle(BulkDeleteTenantsCommand request, CancellationToken cancellationToken) {
    try {
      var tenantIds = request.TenantIds.ToList();

      if (tenantIds.Count == 0) return Result.Success(0);

      var tenants = await context.Resources.OfType<Tenant>()
                                 .Where(t => tenantIds.Contains(t.Id) && t.DeletedAt == null)
                                 .ToListAsync(cancellationToken);

      if (tenants.Count == 0) return Result.Success(0);

      foreach (var tenant in tenants) {
        tenant.SoftDelete();

        // Publish domain event for each deleted tenant
        await eventPublisher.PublishAsync(
          new TenantDeletedEvent(tenant.Id, tenant.Name),
          cancellationToken
        );
      }

      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Bulk deleted {Count} tenants", tenants.Count);

      return Result.Success(tenants.Count);
    }
    catch (Exception ex) {
      logger.LogError(ex, "ErrorMessage bulk deleting tenants");

      return Result.Failure<int>(
        Common.ErrorMessage.Failure("Tenant.BulkDeleteFailed", "Failed to bulk delete tenants")
      );
    }
  }
}
