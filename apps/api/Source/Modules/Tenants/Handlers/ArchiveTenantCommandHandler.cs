using GameGuild.Core.Exceptions;
using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for archiving tenant command
/// </summary>
public class ArchiveTenantCommandHandler(ITenantRepository tenantRepository, ILogger<ArchiveTenantCommandHandler> logger) : ICommandHandler<ArchiveTenantCommand, Result>
{
    public async Task<Result> Handle(ArchiveTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Archiving tenant: {TenantId}", request.TenantId);

            var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                logger.LogWarning("Tenant not found for archiving: {TenantId}", request.TenantId);
                return Result.Failure($"Tenant with ID {request.TenantId} not found");
            }

            if (tenant.IsDefault)
            {
                logger.LogWarning("Cannot archive default tenant: {TenantId}", request.TenantId);
                return Result.Failure("Cannot archive the default tenant");
            }

            if (tenant.IsArchived)
            {
                logger.LogDebug("Tenant is already archived: {TenantId}", request.TenantId);
                return Result.Success("Tenant is already archived");
            }

            tenant.Archive(request.Reason ?? string.Empty);
            await tenantRepository.UpdateAsync(tenant, cancellationToken);

            logger.LogInformation("Successfully archived tenant: {TenantId}", request.TenantId);
            return Result.Success("Tenant archived successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error archiving tenant: {TenantId}", request.TenantId);
            return Result.Failure($"Failed to archive tenant: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for unarchiving tenant command
/// </summary>
public class UnarchiveTenantCommandHandler(ITenantRepository tenantRepository, ILogger<UnarchiveTenantCommandHandler> logger) : ICommandHandler<UnarchiveTenantCommand, Result>
{
    public async Task<Result> Handle(UnarchiveTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Unarchiving tenant: {TenantId}", request.TenantId);

            var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                logger.LogWarning("Tenant not found for unarchiving: {TenantId}", request.TenantId);
                return Result.Failure($"Tenant with ID {request.TenantId} not found");
            }

            if (!tenant.IsArchived)
            {
                logger.LogDebug("Tenant is not archived: {TenantId}", request.TenantId);
                return Result.Success("Tenant is not archived");
            }

            tenant.Unarchive();
            await tenantRepository.UpdateAsync(tenant, cancellationToken);

            logger.LogInformation("Successfully unarchived tenant: {TenantId}", request.TenantId);
            return Result.Success("Tenant unarchived successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unarchiving tenant: {TenantId}", request.TenantId);
            return Result.Failure($"Failed to unarchive tenant: {ex.Message}");
        }
    }
}