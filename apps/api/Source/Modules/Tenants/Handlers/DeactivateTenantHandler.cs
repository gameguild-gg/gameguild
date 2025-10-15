using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for deactivating a tenant
/// </summary>
public class DeactivateTenantHandler(
  ApplicationDbContext context,
  ILogger<DeactivateTenantHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<DeactivateTenantCommand, Common.Result<bool>> {
  public async Task<Common.Result<bool>> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>()
                                .FirstOrDefaultAsync(t => t.Id == request.Id && t.DeletedAt == null, cancellationToken);

      if (tenant == null)
        return Result.Failure<bool>(
          Common.Error.NotFound("Tenant.NotFound", $"Tenant with ID {request.Id} not found")
        );

      if (!tenant.IsActive) return Result.Success(true); // Already inactive

      tenant.IsActive = false;
      tenant.Touch();
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} deactivated successfully", tenant.Id);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new TenantDeactivatedEvent(tenant.Id, tenant.Name),
        cancellationToken
      );

      return Result.Success(true);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error deactivating tenant {TenantId}", request.Id);

      return Result.Failure<bool>(
        Common.Error.Failure("Tenant.DeactivationFailed", "Failed to deactivate tenant")
      );
    }
  }
}
