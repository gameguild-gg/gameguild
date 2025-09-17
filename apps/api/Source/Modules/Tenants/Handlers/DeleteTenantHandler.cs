using GameGuild.CQRS;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary> Handler for soft deleting a tenant </summary>
public class DeleteTenantHandler(ApplicationDbContext context, ILogger<DeleteTenantHandler> logger, IDomainEventPublisher eventPublisher) : ICommandHandler<DeleteTenantCommand, Result<bool>> {
  public async Task<Result<bool>> Handle(DeleteTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>().FirstOrDefaultAsync(t => t.Id == request.Id && t.DeletedAt == null, cancellationToken);

      if (tenant == null) return Result.Failure<bool>(Error.NotFound("Tenant.NotFound", $"Tenant with ID {request.Id} not found"));

      tenant.SoftDelete();
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} soft deleted successfully", tenant.Id);

      // Publish domain event
      await eventPublisher.PublishAsync(new TenantDeletedEvent(tenant.Id, tenant.Name), cancellationToken);

      return Result.Success(true);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error deleting tenant {TenantId}", request.Id);

      return Result.Failure<bool>(Error.Failure("Tenant.DeleteFailed", "Failed to delete tenant"));
    }
  }
}
