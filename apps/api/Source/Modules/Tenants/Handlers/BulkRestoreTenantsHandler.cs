using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for bulk restoring tenants
/// </summary>
public class BulkRestoreTenantsHandler(
  ApplicationDbContext context,
  ILogger<BulkRestoreTenantsHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<BulkRestoreTenantsCommand, Common.Result<int>>
{
  public async Task<Common.Result<int>> Handle(BulkRestoreTenantsCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var tenantIds = request.TenantIds.ToList();
      if (!tenantIds.Any())
      {
        return Result.Success(0);
      }

      var tenants = await context.Resources.OfType<Tenant>()
                                 .Where(t => tenantIds.Contains(t.Id) && t.DeletedAt != null)
                                 .ToListAsync(cancellationToken);

      if (!tenants.Any())
      {
        return Result.Success(0);
      }

      foreach (var tenant in tenants)
      {
        tenant.Restore();
                
        // Publish domain event for each restored tenant
        await eventPublisher.PublishAsync(
          new TenantRestoredEvent(tenant.Id, tenant.Name),
          cancellationToken
        );
      }

      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Bulk restored {Count} tenants", tenants.Count);
      return Result.Success(tenants.Count);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error bulk restoring tenants");
      return Result.Failure<int>(
        Common.Error.Failure("Tenant.BulkRestoreFailed", "Failed to bulk restore tenants")
      );
    }
  }
}
