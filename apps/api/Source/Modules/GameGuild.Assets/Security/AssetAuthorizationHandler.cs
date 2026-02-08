using GameGuild.Identity.Authorization.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace GameGuild.Assets.Security;

/// <summary>
/// Authorization handler interface for asset-specific access control.
/// Encapsulates DAC-based authorization logic for assets.
/// </summary>
public interface IAssetAuthorizationHandler
{
    /// <summary>
    /// Checks if the current actor can read the specified asset.
    /// </summary>
    Task<bool> CanReadAsync(Guid assetId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor can create assets.
    /// </summary>
    Task<bool> CanCreateAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor can update the specified asset.
    /// </summary>
    Task<bool> CanUpdateAsync(Guid assetId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor can delete the specified asset.
    /// </summary>
    Task<bool> CanDeleteAsync(Guid assetId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor can transform the specified asset.
    /// </summary>
    Task<bool> CanTransformAsync(Guid assetId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor can generate access URLs for the specified asset.
    /// </summary>
    Task<bool> CanGenerateUrlAsync(Guid assetId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor can report an asset for moderation.
    /// </summary>
    Task<bool> CanReportAsync(Guid assetId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor has admin permissions for assets.
    /// </summary>
    Task<bool> IsAdminAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if the current actor can moderate assets.
    /// </summary>
    Task<bool> CanModerateAsync(CancellationToken ct = default);
}

/// <summary>
/// ASP.NET Core authorization requirement for asset access.
/// </summary>
public sealed class AssetAccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the required permission for this authorization check.
    /// </summary>
    public Permission RequiredPermission { get; }

    /// <summary>
    /// Gets whether ownership should be checked as an alternative to permission.
    /// </summary>
    public bool AllowOwnerAccess { get; }

    /// <summary>
    /// Creates a new asset access requirement.
    /// </summary>
    public AssetAccessRequirement(Permission requiredPermission, bool allowOwnerAccess = true)
    {
        RequiredPermission = requiredPermission;
        AllowOwnerAccess = allowOwnerAccess;
    }

    /// <summary>Requirement for reading assets (owner or permission).</summary>
    public static readonly AssetAccessRequirement Read = new(AssetsPermission.Read);

    /// <summary>Requirement for creating assets.</summary>
    public static readonly AssetAccessRequirement Create = new(AssetsPermission.Create, allowOwnerAccess: false);

    /// <summary>Requirement for updating assets (owner or permission).</summary>
    public static readonly AssetAccessRequirement Update = new(AssetsPermission.Update);

    /// <summary>Requirement for deleting assets (owner or permission).</summary>
    public static readonly AssetAccessRequirement Delete = new(AssetsPermission.Delete);

    /// <summary>Requirement for admin operations (no owner bypass).</summary>
    public static readonly AssetAccessRequirement Admin = new(AssetsPermission.Admin, allowOwnerAccess: false);

    /// <summary>Requirement for moderation (no owner bypass).</summary>
    public static readonly AssetAccessRequirement Moderate = new(AssetsPermission.Moderate, allowOwnerAccess: false);
}

/// <summary>
/// Implementation of asset authorization using DAC via IAuthorizationService.
/// </summary>
public sealed class AssetAuthorizationHandler : AuthorizationHandler<AssetAccessRequirement>, IAssetAuthorizationHandler
{
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly IAssetReferenceRepository _referenceRepository;
    private readonly IAccessControlListService _aclService;
    private readonly ILogger<AssetAuthorizationHandler> _logger;

    public AssetAuthorizationHandler(
        IActorContextAccessor actorContextAccessor,
        IAssetReferenceRepository referenceRepository,
        IAccessControlListService aclService,
        ILogger<AssetAuthorizationHandler> logger)
    {
        _actorContextAccessor = actorContextAccessor;
        _referenceRepository = referenceRepository;
        _aclService = aclService;
        _logger = logger;
    }

    private ActorContext Actor => _actorContextAccessor.ActorContext;

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AssetAccessRequirement requirement)
    {
        var actor = Actor;
        if (!actor.IsAuthenticated)
        {
            _logger.LogDebug("No actor context available for authorization");
            return;
        }

        // Check if actor has the required permission directly
        if (actor.HasPermission(requirement.RequiredPermission.Key))
        {
            _logger.LogDebug("Asset access granted via permission {Permission}", requirement.RequiredPermission.Key);
            context.Succeed(requirement);
            return;
        }

        // Check ownership if allowed and resource is provided
        if (requirement.AllowOwnerAccess && context.Resource is AssetReference asset)
        {
            if (asset.CreatedByUserId.ToString() == actor.SubjectId)
            {
                _logger.LogDebug("Asset access granted via ownership for asset {AssetId}", asset.Id);
                context.Succeed(requirement);
                return;
            }
        }

        // Check ACL-based access
        if (context.Resource is AssetReference aclAsset && actor.TenantId.HasValue)
        {
            var subject = AclSubject.ForUser(
                userId: actor.SubjectIdAsGuid ?? Guid.Empty,
                roleIds: null, // Roles are checked via permissions
                groupIds: null);

            var minLevel = MapPermissionToAccessLevel(requirement.RequiredPermission);
            var hasAccess = await _aclService.HasAccessAsync(
                subject,
                actor.TenantId.Value,
                ResourceTypes.Asset.Value,
                aclAsset.Id.ToString(),
                minLevel,
                CancellationToken.None);

            if (hasAccess)
            {
                _logger.LogDebug("Asset access granted via ACL for asset {AssetId}", aclAsset.Id);
                context.Succeed(requirement);
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanReadAsync(Guid assetId, CancellationToken ct = default)
    {
        return await CheckAccessAsync(assetId, AssetsPermission.Read, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> CanCreateAsync(CancellationToken ct = default)
    {
        var actor = Actor;
        return Task.FromResult(actor.IsAuthenticated && actor.HasPermission(AssetsPermission.Create.Key));
    }

    /// <inheritdoc />
    public async Task<bool> CanUpdateAsync(Guid assetId, CancellationToken ct = default)
    {
        return await CheckAccessAsync(assetId, AssetsPermission.Update, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CanDeleteAsync(Guid assetId, CancellationToken ct = default)
    {
        return await CheckAccessAsync(assetId, AssetsPermission.Delete, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CanTransformAsync(Guid assetId, CancellationToken ct = default)
    {
        return await CheckAccessAsync(assetId, AssetsPermission.Transform, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CanGenerateUrlAsync(Guid assetId, CancellationToken ct = default)
    {
        return await CheckAccessAsync(assetId, AssetsPermission.GenerateUrl, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CanReportAsync(Guid assetId, CancellationToken ct = default)
    {
        return await CheckAccessAsync(assetId, AssetsPermission.Report, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> IsAdminAsync(CancellationToken ct = default)
    {
        var actor = Actor;
        return Task.FromResult(actor.IsAuthenticated && actor.HasPermission(AssetsPermission.Admin.Key));
    }

    /// <inheritdoc />
    public Task<bool> CanModerateAsync(CancellationToken ct = default)
    {
        var actor = Actor;
        return Task.FromResult(actor.IsAuthenticated && actor.HasPermission(AssetsPermission.Moderate.Key));
    }

    private async Task<bool> CheckAccessAsync(Guid assetId, Permission permission, CancellationToken ct)
    {
        var actor = Actor;
        if (!actor.IsAuthenticated)
        {
            return false;
        }

        // Direct permission check
        if (actor.HasPermission(permission.Key))
        {
            return true;
        }

        // Ownership check
        var asset = await _referenceRepository.GetByIdAsync(assetId, ct).ConfigureAwait(false);
        if (asset == null)
        {
            return false;
        }

        if (asset.CreatedByUserId.ToString() == actor.SubjectId)
        {
            return true;
        }

        // ACL check
        if (actor.TenantId.HasValue)
        {
            var subject = AclSubject.ForUser(
                userId: actor.SubjectIdAsGuid ?? Guid.Empty);

            var minLevel = MapPermissionToAccessLevel(permission);
            return await _aclService.HasAccessAsync(
                subject,
                actor.TenantId.Value,
                ResourceTypes.Asset.Value,
                assetId.ToString(),
                minLevel,
                ct);
        }

        return false;
    }

    private static AccessLevel MapPermissionToAccessLevel(Permission permission)
    {
        return permission.Key switch
        {
            AssetsPermission.Keys.Read => AccessLevel.Read,
            AssetsPermission.Keys.Create => AccessLevel.Write,
            AssetsPermission.Keys.Update => AccessLevel.Write,
            AssetsPermission.Keys.Delete => AccessLevel.Admin, // Delete requires Admin level
            AssetsPermission.Keys.Admin => AccessLevel.Admin,
            AssetsPermission.Keys.Moderate => AccessLevel.Admin,
            AssetsPermission.Keys.Transform => AccessLevel.Read,
            AssetsPermission.Keys.GenerateUrl => AccessLevel.Read,
            AssetsPermission.Keys.Report => AccessLevel.Read,
            _ => AccessLevel.None
        };
    }
}
