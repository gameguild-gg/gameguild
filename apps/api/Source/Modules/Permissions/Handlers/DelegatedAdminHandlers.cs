using GameGuild.Modules.Permissions.Entities;
using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;

namespace GameGuild.Modules.Permissions.Handlers;

/// <summary>
/// Handler for creating delegated admin scopes
/// </summary>
public class CreateDelegatedAdminHandler : IRequestHandler<CreateDelegatedAdminCommand, Result<DelegatedAdminScope>>
{
    private readonly IDelegatedAdminService _delegatedAdminService;
    private readonly ILogger<CreateDelegatedAdminHandler> _logger;

    public CreateDelegatedAdminHandler(
        IDelegatedAdminService delegatedAdminService,
        ILogger<CreateDelegatedAdminHandler> logger)
    {
        _delegatedAdminService = delegatedAdminService ?? throw new ArgumentNullException(nameof(delegatedAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DelegatedAdminScope>> Handle(CreateDelegatedAdminCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var delegation = await _delegatedAdminService.CreateDelegationAsync(
                request.DelegatorUserId,
                request.DelegatedUserId,
                request.TenantId,
                request.ScopeType,
                request.ScopeId,
                request.ScopeName,
                request.Permissions,
                request.AllowSubDelegation,
                request.MaxDelegationDepth,
                request.ExpiresAt,
                request.Reason,
                request.Constraints,
                cancellationToken);

            return Result<DelegatedAdminScope>.Success(delegation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delegated admin scope");
            return Result<DelegatedAdminScope>.Failure(Error.Failure("CreateDelegationFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for creating sub-delegations
/// </summary>
public class CreateSubDelegationHandler : IRequestHandler<CreateSubDelegationCommand, Result<DelegatedAdminScope>>
{
    private readonly IDelegatedAdminService _delegatedAdminService;
    private readonly ILogger<CreateSubDelegationHandler> _logger;

    public CreateSubDelegationHandler(
        IDelegatedAdminService delegatedAdminService,
        ILogger<CreateSubDelegationHandler> logger)
    {
        _delegatedAdminService = delegatedAdminService ?? throw new ArgumentNullException(nameof(delegatedAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DelegatedAdminScope>> Handle(CreateSubDelegationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var delegation = await _delegatedAdminService.CreateSubDelegationAsync(
                request.ParentDelegationId,
                request.NewDelegatedUserId,
                request.Permissions,
                request.ExpiresAt,
                request.Reason,
                cancellationToken);

            return Result<DelegatedAdminScope>.Success(delegation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sub-delegation");
            return Result<DelegatedAdminScope>.Failure(Error.Failure("CreateSubDelegationFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for revoking delegations
/// </summary>
public class RevokeDelegationHandler : IRequestHandler<RevokeDelegationCommand, Result>
{
    private readonly IDelegatedAdminService _delegatedAdminService;
    private readonly ILogger<RevokeDelegationHandler> _logger;

    public RevokeDelegationHandler(
        IDelegatedAdminService delegatedAdminService,
        ILogger<RevokeDelegationHandler> logger)
    {
        _delegatedAdminService = delegatedAdminService ?? throw new ArgumentNullException(nameof(delegatedAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _delegatedAdminService.RevokeDelegationAsync(
                request.DelegationId,
                request.RevokedByUserId,
                request.Reason,
                request.RevokeSubDelegations,
                cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking delegation");
            return Result.Failure(Error.Failure("RevokeDelegationFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for getting user delegated permissions
/// </summary>
public class GetUserDelegatedPermissionsHandler : IRequestHandler<GetUserDelegatedPermissionsQuery, Result<PermissionType[]>>
{
    private readonly IDelegatedAdminService _delegatedAdminService;
    private readonly ILogger<GetUserDelegatedPermissionsHandler> _logger;

    public GetUserDelegatedPermissionsHandler(
        IDelegatedAdminService delegatedAdminService,
        ILogger<GetUserDelegatedPermissionsHandler> logger)
    {
        _delegatedAdminService = delegatedAdminService ?? throw new ArgumentNullException(nameof(delegatedAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PermissionType[]>> Handle(GetUserDelegatedPermissionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await _delegatedAdminService.GetDelegatedPermissionsAsync(
                request.UserId,
                request.TenantId,
                request.ScopeType,
                request.ScopeId,
                cancellationToken);

            return Result<PermissionType[]>.Success(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user delegated permissions");
            return Result<PermissionType[]>.Failure(Error.Failure("GetDelegatedPermissionsFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for checking delegated permission
/// </summary>
public class CheckDelegatedPermissionHandler : IRequestHandler<CheckDelegatedPermissionQuery, Result<bool>>
{
    private readonly IDelegatedAdminService _delegatedAdminService;
    private readonly ILogger<CheckDelegatedPermissionHandler> _logger;

    public CheckDelegatedPermissionHandler(
        IDelegatedAdminService delegatedAdminService,
        ILogger<CheckDelegatedPermissionHandler> logger)
    {
        _delegatedAdminService = delegatedAdminService ?? throw new ArgumentNullException(nameof(delegatedAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<bool>> Handle(CheckDelegatedPermissionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var hasPermission = await _delegatedAdminService.HasDelegatedPermissionAsync(
                request.UserId,
                request.TenantId,
                request.ScopeType,
                request.ScopeId,
                request.Permission,
                cancellationToken);

            return Result<bool>.Success(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking delegated permission");
            return Result<bool>.Failure(Error.Failure("CheckDelegatedPermissionFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for getting delegation statistics
/// </summary>
public class GetDelegationStatisticsHandler : IRequestHandler<GetDelegationStatisticsQuery, Result<DelegationStatistics>>
{
    private readonly IDelegatedAdminService _delegatedAdminService;
    private readonly ILogger<GetDelegationStatisticsHandler> _logger;

    public GetDelegationStatisticsHandler(
        IDelegatedAdminService delegatedAdminService,
        ILogger<GetDelegationStatisticsHandler> logger)
    {
        _delegatedAdminService = delegatedAdminService ?? throw new ArgumentNullException(nameof(delegatedAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DelegationStatistics>> Handle(GetDelegationStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _delegatedAdminService.GetDelegationStatisticsAsync(
                request.TenantId,
                cancellationToken);

            return Result<DelegationStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delegation statistics");
            return Result<DelegationStatistics>.Failure(Error.Failure("GetDelegationStatisticsFailed", ex.Message));
        }
    }
}
