using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for creating a new tenant
/// </summary>
public class CreateTenantHandler(ApplicationDbContext context, ILogger<CreateTenantHandler> logger, IDomainEventPublisher eventPublisher) : ICommandHandler<CreateTenantCommand, Common.Result<Tenant>> {
  public async Task<Common.Result<Tenant>> Handle(CreateTenantCommand request, CancellationToken cancellationToken) {
    try {
      // Check if tenant with same name already exists
      var existingTenant = await context.Resources.OfType<Tenant>()
                                        .FirstOrDefaultAsync(t => t.Name == request.Name && t.DeletedAt == null, cancellationToken);

      if (existingTenant != null)
        return Result.Failure<Tenant>(
          Common.Error.Conflict("Tenant.NameExists", $"Tenant with name '{request.Name}' already exists")
        );

      // Check if tenant with same slug already exists
      var existingSlug = await context.Resources.OfType<Tenant>()
                                      .FirstOrDefaultAsync(t => t.Slug == request.Slug && t.DeletedAt == null, cancellationToken);

      if (existingSlug != null)
        return Result.Failure<Tenant>(
          Common.Error.Conflict("Tenant.SlugExists", $"Tenant with slug '{request.Slug}' already exists")
        );

      var tenant = new Tenant {
        Name = request.Name,
        Description = request.Description,
        IsActive = request.IsActive,
        Slug = request.Slug,
      };

      context.Resources.Add(tenant);
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Tenant {TenantId} with name '{TenantName}' created successfully", tenant.Id, tenant.Name);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new TenantCreatedEvent(tenant.Id, tenant.Name, tenant.Description, tenant.IsActive, tenant.Slug),
        cancellationToken
      );

      return Result.Success(tenant);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error creating tenant with name '{TenantName}'", request.Name);

      return Result.Failure<Tenant>(
        Common.Error.Failure("Tenant.CreateFailed", "Failed to create tenant")
      );
    }
  }
}
