using GameGuild.CQRS;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for activating a tenant
/// </summary>
public class ActivateTenantHandler(ApplicationDbContext context, ILogger<ActivateTenantHandler> logger, IDomainEventPublisher eventPublisher) : ICommandHandler<ActivateTenantCommand, Result<bool>> {
  public async Task<Result<bool>> Handle(ActivateTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>().FirstOrDefaultAsync(t => t.Id == request.Id && t.DeletedAt == null, cancellationToken);

      if (tenant == null)
        return Result.Failure<bool>(
          Error.NotFound("Tenant.NotFound", $"Tenant with ID {request.Id} not found")
        );

      if (tenant.IsActive) return Result.Success(true); // Already active

      tenant.IsActive = true;
      tenant.Touch();
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} activated successfully", tenant.Id);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new TenantActivatedEvent(tenant.Id, tenant.Name),
        cancellationToken
      );

      return Result.Success(true);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error activating tenant {TenantId}", request.Id);

      return Result.Failure<bool>(
        Error.Failure("Tenant.ActivationFailed", "Failed to activate tenant")
      );
    }
  }
}
