using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for soft deleting a tenant
/// </summary>
public class DeleteTenantHandler(
  ApplicationDbContext context,
  ILogger<DeleteTenantHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<DeleteTenantCommand, Common.Result<bool>> {
  public async Task<Common.Result<bool>> Handle(DeleteTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>()
                                .FirstOrDefaultAsync(t => t.Id == request.Id && t.DeletedAt == null, cancellationToken);

      if (tenant == null)
        return Result.Failure<bool>(
          Common.Error.NotFound("Tenant.NotFound", $"Tenant with ID {request.Id} not found")
        );

      tenant.SoftDelete();
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} soft deleted successfully", tenant.Id);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new TenantDeletedEvent(tenant.Id, tenant.Name),
        cancellationToken
      );

      return Result.Success(true);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error deleting tenant {TenantId}", request.Id);

      return Result.Failure<bool>(
        Common.Error.Failure("Tenant.DeleteFailed", "Failed to delete tenant")
      );
    }
  }
}
