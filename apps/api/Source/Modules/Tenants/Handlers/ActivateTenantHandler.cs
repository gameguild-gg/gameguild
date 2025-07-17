using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for activating a tenant
/// </summary>
public class ActivateTenantHandler(ApplicationDbContext context, ILogger<ActivateTenantHandler> logger, IDomainEventPublisher eventPublisher) : ICommandHandler<ActivateTenantCommand, Common.Result<bool>> {
  public async Task<Common.Result<bool>> Handle(ActivateTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>().FirstOrDefaultAsync(t => t.Id == request.Id && t.DeletedAt == null, cancellationToken);

      if (tenant == null)
        return Result.Failure<bool>(
          Common.ErrorMessage.PageNotFound("Tenant.PageNotFound", $"Tenant with ID {request.Id} not found")
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
      logger.LogError(ex, "ErrorMessage activating tenant {TenantId}", request.Id);

      return Result.Failure<bool>(
        Common.ErrorMessage.Failure("Tenant.ActivationFailed", "Failed to activate tenant")
      );
    }
  }
}
