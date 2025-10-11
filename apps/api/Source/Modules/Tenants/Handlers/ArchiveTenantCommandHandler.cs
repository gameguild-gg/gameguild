using GameGuild.Core.Exceptions;
using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for archiving tenant command
/// </summary>
public class ArchiveTenantCommandHandler(ITenantRepository tenantRepository, ILogger<ArchiveTenantCommandHandler> logger) : IRequestHandler<ArchiveTenantCommand, Result<TenantArchiveDto>>
{
    public async Task<Result<TenantArchiveDto>> Handle(ArchiveTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Archiving tenant: {TenantId}", request.TenantId);

            var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                logger.LogWarning("Tenant not found for archiving: {TenantId}", request.TenantId);
                return Result<TenantArchiveDto>.Failure($"Tenant with ID {request.TenantId} not found");
            }

            if (tenant.IsDefault)
            {
                logger.LogWarning("Cannot archive default tenant: {TenantId}", request.TenantId);
                return Result<TenantArchiveDto>.Failure("Cannot archive the default tenant");
            }

            if (tenant.IsArchived)
            {
                logger.LogDebug("Tenant is already archived: {TenantId}", request.TenantId);
                var existingDto = new TenantArchiveDto(
                    tenant.Id,
                    tenant.ArchivedAt ?? DateTime.UtcNow,
                    tenant.ArchivedReason ?? string.Empty
                );
                return Result<TenantArchiveDto>.Success(existingDto);
            }

            tenant.Archive(request.Reason ?? string.Empty);
            await tenantRepository.UpdateAsync(tenant, cancellationToken);

            var dto = new TenantArchiveDto(
                tenant.Id,
                tenant.ArchivedAt ?? DateTime.UtcNow,
                tenant.ArchivedReason ?? string.Empty
            );

            logger.LogInformation("Successfully archived tenant: {TenantId}", request.TenantId);
            return Result<TenantArchiveDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error archiving tenant: {TenantId}", request.TenantId);
            return Result<TenantArchiveDto>.Failure($"Failed to archive tenant: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for unarchiving tenant command
/// </summary>
public class UnarchiveTenantCommandHandler(ITenantRepository tenantRepository, ILogger<UnarchiveTenantCommandHandler> logger) : IRequestHandler<UnarchiveTenantCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UnarchiveTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Unarchiving tenant: {TenantId}", request.TenantId);

            var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                logger.LogWarning("Tenant not found for unarchiving: {TenantId}", request.TenantId);
                return Result<bool>.Failure($"Tenant with ID {request.TenantId} not found");
            }

            if (!tenant.IsArchived)
            {
                logger.LogDebug("Tenant is not archived: {TenantId}", request.TenantId);
                return Result<bool>.Success(false);
            }

            tenant.Unarchive();
            await tenantRepository.UpdateAsync(tenant, cancellationToken);

            logger.LogInformation("Successfully unarchived tenant: {TenantId}", request.TenantId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unarchiving tenant: {TenantId}", request.TenantId);
            return Result<bool>.Failure($"Failed to unarchive tenant: {ex.Message}");
        }
    }
}