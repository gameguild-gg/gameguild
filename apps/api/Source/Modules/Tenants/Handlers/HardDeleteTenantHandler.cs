using GameGuild.CQRS;
using GameGuild.Database;

namespace GameGuild.Modules.Tenants;

/// <summary> Handler for permanently deleting a tenant </summary>
public class HardDeleteTenantHandler(ApplicationDbContext context, ILogger<HardDeleteTenantHandler> logger) : ICommandHandler<HardDeleteTenantCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(HardDeleteTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await context.Tenants.OfType<Tenant>().FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tenant == null) return Result.Failure<bool>(Error.NotFound("Tenant.NotFound", $"Tenant with ID {request.Id} not found"));

            context.Tenants.Remove(tenant);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Tenant {TenantId} permanently deleted", tenant.Id);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error permanently deleting tenant {TenantId}", request.Id);

            return Result.Failure<bool>(Error.Failure("Tenant.HardDeleteFailed", "Failed to permanently delete tenant"));
        }
    }
}
