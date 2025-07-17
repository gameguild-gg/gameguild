using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for updating an existing tenant
/// </summary>
public class UpdateTenantHandler(ApplicationDbContext context, ILogger<UpdateTenantHandler> logger, IDomainEventPublisher eventPublisher) : ICommandHandler<UpdateTenantCommand, Common.Result<Tenant>> {
  public async Task<Common.Result<Tenant>> Handle(UpdateTenantCommand request, CancellationToken cancellationToken) {
    try {
      var tenant = await context.Resources.OfType<Tenant>().FirstOrDefaultAsync(t => t.Id == request.Id && t.DeletedAt == null, cancellationToken);

      if (tenant == null)
        return Result.Failure<Tenant>(
          Common.ErrorMessage.PageNotFound("Tenant.PageNotFound", $"Tenant with ID {request.Id} not found")
        );

      // Check if new name conflicts with another tenant
      if (!string.IsNullOrEmpty(request.Name) && tenant.Name != request.Name) {
        var existingTenant = await context.Resources.OfType<Tenant>()
                                          .FirstOrDefaultAsync(t => t.Name == request.Name && t.Id != request.Id && t.DeletedAt == null, cancellationToken);

        if (existingTenant != null)
          return Result.Failure<Tenant>(
            Common.ErrorMessage.Conflict("Tenant.NameExists", $"Tenant with name '{request.Name}' already exists")
          );
      }

      // Check if new slug conflicts with another tenant
      if (!string.IsNullOrEmpty(request.Slug) && tenant.Slug != request.Slug) {
        var existingSlug = await context.Resources.OfType<Tenant>()
                                        .FirstOrDefaultAsync(t => t.Slug == request.Slug && t.Id != request.Id && t.DeletedAt == null, cancellationToken);

        if (existingSlug != null)
          return Result.Failure<Tenant>(
            Common.ErrorMessage.Conflict("Tenant.SlugExists", $"Tenant with slug '{request.Slug}' already exists")
          );
      }

      // Update tenant properties (only if provided)
      if (!string.IsNullOrEmpty(request.Name)) tenant.Name = request.Name;
      if (request.Description != null) tenant.Description = request.Description;
      if (request.IsActive.HasValue) tenant.IsActive = request.IsActive.Value;
      if (!string.IsNullOrEmpty(request.Slug)) tenant.Slug = request.Slug;
      tenant.Touch();

      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} updated successfully", tenant.Id);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new TenantUpdatedEvent(tenant.Id, tenant.Name, tenant.Description, tenant.IsActive, tenant.Slug),
        cancellationToken
      );

      return Result.Success(tenant);
    }
    catch (Exception ex) {
      logger.LogError(ex, "ErrorMessage updating tenant {TenantId}", request.Id);

      return Result.Failure<Tenant>(
        Common.ErrorMessage.Failure("Tenant.UpdateFailed", "Failed to update tenant")
      );
    }
  }
}
