using Microsoft.Extensions.Logging;

namespace GameGuild.Assets.Security;

/// <summary>
/// Options for tenant isolation in assets.
/// Mitigates: Tenant Confusion (#6)
/// </summary>
public class TenantIsolationOptions
{
    public const string SectionName = "Assets:TenantIsolation";

    /// <summary>
    /// Whether to fail-closed on missing tenant context.
    /// CRITICAL: Should always be true in production.
    /// </summary>
    public bool FailClosedOnMissingTenant { get; set; } = true;

    /// <summary>
    /// Whether to validate tenant ID in tokens.
    /// </summary>
    public bool ValidateTenantInToken { get; set; } = true;

    /// <summary>
    /// Whether to allow cross-tenant asset access for admins.
    /// </summary>
    public bool AllowCrossTenantForAdmins { get; set; } = false;

    /// <summary>
    /// Tenant IDs that have global access (system tenants).
    /// </summary>
    public Guid[] GlobalAccessTenants { get; set; } = [];
}

/// <summary>
/// Result of tenant validation.
/// </summary>
public record TenantValidationResult(
    bool IsValid,
    string? Error = null,
    Guid? ResolvedTenantId = null);

/// <summary>
/// Service for enforcing tenant isolation in asset access.
/// </summary>
public interface ITenantAssetValidationService
{
    /// <summary>
    /// Validates that the current context has proper tenant access.
    /// FAIL-CLOSED: Returns false if tenant cannot be validated.
    /// </summary>
    TenantValidationResult ValidateTenantAccess(
        Guid? requestedTenantId,
        Guid assetTenantId,
        ActorContext actor);

    /// <summary>
    /// Validates that a token's tenant matches the request context.
    /// </summary>
    TenantValidationResult ValidateTokenTenant(
        Guid tokenTenantId,
        Guid? contextTenantId);

    /// <summary>
    /// Resolves the effective tenant ID for an operation.
    /// </summary>
    TenantValidationResult ResolveEffectiveTenant(
        Guid? requestTenantId,
        ActorContext actor);
}

/// <summary>
/// Implementation of tenant validation with fail-closed behavior.
/// </summary>
public class TenantAssetValidationService : ITenantAssetValidationService
{
    private readonly TenantIsolationOptions _options;
    private readonly ILogger<TenantAssetValidationService> _logger;

    public TenantAssetValidationService(
        Microsoft.Extensions.Options.IOptions<TenantIsolationOptions> options,
        ILogger<TenantAssetValidationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public TenantValidationResult ValidateTenantAccess(
        Guid? requestedTenantId,
        Guid assetTenantId,
        ActorContext actor)
    {
        // FAIL-CLOSED: If no actor context, deny access
        if (actor == null)
        {
            _logger.LogWarning("Tenant validation failed: No actor context");
            return new TenantValidationResult(false, "No actor context available");
        }

        // FAIL-CLOSED: If asset has no tenant but we require it, deny
        if (assetTenantId == Guid.Empty && _options.FailClosedOnMissingTenant)
        {
            _logger.LogWarning(
                "Tenant validation failed: Asset has no tenant and fail-closed is enabled");
            return new TenantValidationResult(false, "Asset has no tenant context");
        }

        // Check for global access tenants
        if (actor.TenantId.HasValue && 
            _options.GlobalAccessTenants.Contains(actor.TenantId.Value))
        {
            return new TenantValidationResult(true, null, assetTenantId);
        }

        // Check actor's tenant matches asset's tenant
        if (!actor.TenantId.HasValue)
        {
            if (_options.FailClosedOnMissingTenant)
            {
                _logger.LogWarning(
                    "Tenant validation failed: No tenant in actor context (fail-closed)");
                return new TenantValidationResult(false, "No tenant in request context");
            }
        }
        else if (actor.TenantId.Value != assetTenantId)
        {
            // Cross-tenant access attempt
            if (_options.AllowCrossTenantForAdmins && actor.IsSystemAdmin)
            {
                _logger.LogInformation(
                    "Cross-tenant access allowed for admin {UserId}: {ActorTenant} -> {AssetTenant}",
                    actor.SubjectId, actor.TenantId, assetTenantId);
                return new TenantValidationResult(true, null, assetTenantId);
            }

            _logger.LogWarning(
                "Tenant validation failed: Actor tenant {ActorTenant} does not match asset tenant {AssetTenant}",
                actor.TenantId, assetTenantId);
            return new TenantValidationResult(false, "Tenant mismatch");
        }

        return new TenantValidationResult(true, null, assetTenantId);
    }

    public TenantValidationResult ValidateTokenTenant(
        Guid tokenTenantId,
        Guid? contextTenantId)
    {
        if (!_options.ValidateTenantInToken)
        {
            return new TenantValidationResult(true, null, tokenTenantId);
        }

        // FAIL-CLOSED: Token must have tenant
        if (tokenTenantId == Guid.Empty)
        {
            if (_options.FailClosedOnMissingTenant)
            {
                _logger.LogWarning("Token validation failed: Token has no tenant");
                return new TenantValidationResult(false, "Token has no tenant");
            }
            return new TenantValidationResult(true, null, null);
        }

        // If context has tenant, it must match token
        if (contextTenantId.HasValue && contextTenantId.Value != tokenTenantId)
        {
            _logger.LogWarning(
                "Token validation failed: Token tenant {TokenTenant} does not match context {ContextTenant}",
                tokenTenantId, contextTenantId);
            return new TenantValidationResult(false, "Token tenant mismatch");
        }

        return new TenantValidationResult(true, null, tokenTenantId);
    }

    public TenantValidationResult ResolveEffectiveTenant(
        Guid? requestTenantId,
        ActorContext actor)
    {
        // Priority: Request > Actor context
        if (requestTenantId.HasValue && requestTenantId.Value != Guid.Empty)
        {
            // Validate actor can access this tenant
            if (actor.TenantId.HasValue && 
                actor.TenantId.Value != requestTenantId.Value &&
                !_options.GlobalAccessTenants.Contains(actor.TenantId.Value) &&
                !(actor.IsSystemAdmin && _options.AllowCrossTenantForAdmins))
            {
                return new TenantValidationResult(
                    false, 
                    "Cannot access resources in different tenant");
            }
            return new TenantValidationResult(true, null, requestTenantId.Value);
        }

        if (actor.TenantId.HasValue)
        {
            return new TenantValidationResult(true, null, actor.TenantId.Value);
        }

        if (_options.FailClosedOnMissingTenant)
        {
            return new TenantValidationResult(false, "No tenant context available");
        }

        return new TenantValidationResult(true, null, null);
    }
}
