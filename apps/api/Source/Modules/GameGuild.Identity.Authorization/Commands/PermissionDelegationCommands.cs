using FluentValidation;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Commands;

// ============================================================================
// Permission Delegation Commands
// ============================================================================

/// <summary>
///     Command to delegate permissions to another user
/// </summary>
public record DelegatePermissionsCommand(
    Guid DelegatorUserId,
    Guid DelegateUserId,
    string[] Permissions,
    Guid? TenantId,
    Guid? ResourceId = null,
    DateTime? ExpiresAt = null,
    bool CanSubDelegate = false,
    string? Reason = null,
    int? UsageLimit = null
) : ICommand<PermissionDelegation>;

public class DelegatePermissionsValidator : AbstractValidator<DelegatePermissionsCommand>
{
    public DelegatePermissionsValidator()
    {
        RuleFor(x => x.DelegatorUserId).NotEmpty();
        RuleFor(x => x.DelegateUserId).NotEmpty();
        RuleFor(x => x.Permissions).NotEmpty();
        RuleFor(x => x.DelegatorUserId).NotEqual(x => x.DelegateUserId)
            .WithMessage("Cannot delegate permissions to yourself");
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future");
        RuleFor(x => x.UsageLimit).GreaterThan(0)
            .When(x => x.UsageLimit.HasValue);
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => x.Reason != null);
    }
}

public class DelegatePermissionsHandler(IPermissionDelegationService service)
    : ICommandHandler<DelegatePermissionsCommand, PermissionDelegation>
{
    public async Task<PermissionDelegation> Handle(
        DelegatePermissionsCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.DelegatePermissionsAsync(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.Permissions,
            request.TenantId,
            request.ResourceId,
            request.ExpiresAt,
            request.CanSubDelegate,
            request.Reason,
            request.UsageLimit,
            cancellationToken
        );
    }
}

/// <summary>
///     Command to revoke a permission delegation
/// </summary>
public record RevokeDelegationCommand(Guid DelegationId) : ICommand<bool>;

public class RevokeDelegationValidator : AbstractValidator<RevokeDelegationCommand>
{
    public RevokeDelegationValidator()
    {
        RuleFor(x => x.DelegationId).NotEmpty();
    }
}

public class RevokeDelegationHandler(IPermissionDelegationService service)
    : ICommandHandler<RevokeDelegationCommand, bool>
{
    public async Task<bool> Handle(
        RevokeDelegationCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.RevokeDelegationAsync(request.DelegationId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to record usage of a delegated permission
/// </summary>
public record RecordDelegationUsageCommand(Guid DelegationId) : ICommand<bool>;

public class RecordDelegationUsageHandler(IPermissionDelegationService service)
    : ICommandHandler<RecordDelegationUsageCommand, bool>
{
    public async Task<bool> Handle(
        RecordDelegationUsageCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.RecordDelegationUsageAsync(request.DelegationId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to cleanup expired delegations
/// </summary>
public record CleanupExpiredDelegationsCommand : ICommand<int>;

public class CleanupExpiredDelegationsHandler(IPermissionDelegationService service)
    : ICommandHandler<CleanupExpiredDelegationsCommand, int>
{
    public async Task<int> Handle(
        CleanupExpiredDelegationsCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.CleanupExpiredDelegationsAsync(cancellationToken).ConfigureAwait(false);
    }
}
