using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for restoring a soft-deleted tenant
/// </summary>
public class RestoreTenantHandler(
  ApplicationDbContext context,
  ILogger<RestoreTenantHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<RestoreTenantCommand, Common.Result<bool>> {
  public async Task<Common.Result<bool>> Handle(RestoreTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>()
                                .FirstOrDefaultAsync(t => t.Id == request.Id && t.DeletedAt != null, cancellationToken);

      if (tenant == null) {
        return Result.Failure<bool>(
          Common.Error.NotFound("Tenant.NotFound", $"Deleted tenant with ID {request.Id} not found")
        );
      }

      tenant.Restore();
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} restored successfully", tenant.Id);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new TenantRestoredEvent(tenant.Id, tenant.Name),
        cancellationToken
      );

      return Result.Success(true);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error restoring tenant {TenantId}", request.Id);

      return Result.Failure<bool>(
        Common.Error.Failure("Tenant.RestoreFailed", "Failed to restore tenant")
      );
    }
  }
}
