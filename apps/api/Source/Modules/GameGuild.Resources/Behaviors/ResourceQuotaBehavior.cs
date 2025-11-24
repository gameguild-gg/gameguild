using System.Reflection;
using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Attributes;
using GameGuild.Resources.Exceptions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources.Behaviors;

/// <summary>
///     Pipeline behavior that automatically validates and enforces resource quotas
///     for commands decorated with the [RequiresQuota] attribute.
/// </summary>
/// <typeparam name="TRequest">The request type (command)</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class ResourceQuotaBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequestBase
{
    private readonly IResourceQuotaService _quotaService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ResourceQuotaBehavior<TRequest, TResponse>> _logger;

    public ResourceQuotaBehavior(
        IResourceQuotaService quotaService,
        ITenantContext tenantContext,
        ILogger<ResourceQuotaBehavior<TRequest, TResponse>> logger)
    {
        _quotaService = quotaService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if the command requires quota validation
        var quotaAttribute = typeof(TRequest).GetCustomAttribute<RequiresQuotaAttribute>();

        if (quotaAttribute == null)
        {
            // No quota required, proceed normally
            return await next();
        }

        // Ensure we have a tenant context
        if (!_tenantContext.TenantId.HasValue)
        {
            _logger.LogWarning(
                "Command {CommandType} requires quota validation but no tenant context is available. Skipping quota check.",
                typeof(TRequest).Name
            );
            return await next();
        }

        var tenantId = _tenantContext.TenantId.Value;
        var resourceType = quotaAttribute.ResourceType;
        var amount = quotaAttribute.Amount;
        var source = quotaAttribute.Source ?? typeof(TRequest).Name;

        _logger.LogDebug(
            "Checking resource quota for tenant {TenantId}, resource {ResourceType}, amount {Amount}",
            tenantId,
            resourceType,
            amount
        );

        try
        {
            // Check if the tenant can consume the requested amount
            var limitCheck = await _quotaService.CheckLimitsAsync(
                tenantId,
                resourceType,
                amount,
                cancellationToken
            );

            // Handle soft limit exceeded (warning only)
            if (limitCheck.SoftLimit.HasValue &&
                (limitCheck.CurrentUsage + amount) > limitCheck.SoftLimit.Value)
            {
                _logger.LogWarning(
                    "Tenant {TenantId} is approaching resource quota limit for {ResourceType}. " +
                    "Current: {CurrentUsage}, Soft Limit: {SoftLimit}, Hard Limit: {HardLimit}",
                    tenantId,
                    resourceType,
                    limitCheck.CurrentUsage,
                    limitCheck.SoftLimit,
                    limitCheck.HardLimit
                );
            }

            // Handle hard limit exceeded
            if (!limitCheck.CanProceed)
            {
                var errorMessage = $"Resource quota exceeded for {resourceType}. " +
                                   $"Current usage: {limitCheck.CurrentUsage}, " +
                                   $"Hard limit: {limitCheck.HardLimit}, " +
                                   $"Requested: {amount}";

                _logger.LogError(
                    "Quota exceeded for tenant {TenantId}, resource {ResourceType}. " +
                    "Current: {CurrentUsage}, Limit: {HardLimit}, Requested: {Amount}",
                    tenantId,
                    resourceType,
                    limitCheck.CurrentUsage,
                    limitCheck.HardLimit,
                    amount
                );

                if (quotaAttribute.EnforceHardLimit)
                {
                    throw new QuotaExceededException(
                        errorMessage,
                        resourceType,
                        limitCheck.CurrentUsage,
                        limitCheck.HardLimit ?? 0,
                        tenantId
                    );
                }

                // If not enforcing, just log warning and continue
                _logger.LogWarning("Hard limit not enforced for {ResourceType}, allowing operation to proceed", resourceType);
            }

            // Execute the command
            var response = await next();

            // Record usage after successful execution
            if (quotaAttribute.RecordUsage)
            {
                _logger.LogDebug(
                    "Recording resource usage for tenant {TenantId}, resource {ResourceType}, amount {Amount}",
                    tenantId,
                    resourceType,
                    amount
                );

                // Extract user ID if the response contains it
                Guid? userId = TryExtractUserId(response);

                var recorded = await _quotaService.RecordUsageAsync(
                    tenantId,
                    resourceType,
                    amount,
                    userId,
                    source,
                    metadata: null,
                    cancellationToken
                );

                if (!recorded)
                {
                    _logger.LogWarning(
                        "Failed to record resource usage for tenant {TenantId}, resource {ResourceType}",
                        tenantId,
                        resourceType
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Successfully recorded {Amount} {ResourceType} usage for tenant {TenantId}",
                        amount,
                        resourceType,
                        tenantId
                    );
                }
            }

            return response;
        }
        catch (QuotaExceededException)
        {
            // Re-throw quota exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking or recording quota for tenant {TenantId}, resource {ResourceType}",
                tenantId,
                resourceType
            );

            // Don't fail the operation due to quota service errors
            // This ensures the business operation can proceed even if quota service has issues
            return await next();
        }
    }

    /// <summary>
    ///     Attempts to extract user ID from the response object
    /// </summary>
    private static Guid? TryExtractUserId(TResponse response)
    {
        if (response == null)
            return null;

        // Try to get Id or UserId property via reflection
        var responseType = response.GetType();
        var idProperty = responseType.GetProperty("Id") ?? responseType.GetProperty("UserId");

        if (idProperty != null && idProperty.PropertyType == typeof(Guid))
        {
            return (Guid?)idProperty.GetValue(response);
        }

        return null;
    }
}
