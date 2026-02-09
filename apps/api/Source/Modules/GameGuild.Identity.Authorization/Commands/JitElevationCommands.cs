using FluentValidation;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Commands;

// ============================================================================
// JIT Elevation Commands
// ============================================================================

/// <summary>
///     Command to request a Just-in-Time permission elevation
/// </summary>
public sealed record RequestJitElevationCommand(
    Guid RequesterId,
    Guid? TenantId,
    string Permission,
    string Justification,
    int DurationMinutes,
    Guid? ResourceId = null,
    string? ResourceType = null,
    DateTime? StartsAt = null
) : ICommand<JitElevationRequest>;

public sealed class RequestJitElevationValidator : AbstractValidator<RequestJitElevationCommand>
{
    public RequestJitElevationValidator()
    {
        RuleFor(x => x.RequesterId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Justification).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(1440); // Max 24 hours
        RuleFor(x => x.ResourceType).MaximumLength(128).When(x => x.ResourceType != null);
    }
}

public sealed class RequestJitElevationHandler(IJitElevationService service)
    : ICommandHandler<RequestJitElevationCommand, JitElevationRequest>
{
    public async Task<JitElevationRequest> Handle(
        RequestJitElevationCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.RequestElevationAsync(
            request.RequesterId,
            request.TenantId,
            request.Permission,
            request.Justification,
            request.DurationMinutes,
            request.ResourceId,
            request.ResourceType,
            request.StartsAt,
            cancellationToken
        );
    }
}

/// <summary>
///     Command to approve a JIT elevation request
/// </summary>
public sealed record ApproveJitElevationCommand(
    Guid RequestId,
    Guid ReviewerId,
    string? Comments = null
) : ICommand<JitElevationRequest>;

public sealed class ApproveJitElevationValidator : AbstractValidator<ApproveJitElevationCommand>
{
    public ApproveJitElevationValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments != null);
    }
}

public sealed class ApproveJitElevationHandler(IJitElevationService service)
    : ICommandHandler<ApproveJitElevationCommand, JitElevationRequest>
{
    public async Task<JitElevationRequest> Handle(
        ApproveJitElevationCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.ApproveRequestAsync(
            request.RequestId,
            request.ReviewerId,
            request.Comments,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to deny a JIT elevation request
/// </summary>
public sealed record DenyJitElevationCommand(
    Guid RequestId,
    Guid ReviewerId,
    string Comments
) : ICommand<JitElevationRequest>;

public sealed class DenyJitElevationValidator : AbstractValidator<DenyJitElevationCommand>
{
    public DenyJitElevationValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(2000);
    }
}

public sealed class DenyJitElevationHandler(IJitElevationService service)
    : ICommandHandler<DenyJitElevationCommand, JitElevationRequest>
{
    public async Task<JitElevationRequest> Handle(
        DenyJitElevationCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.DenyRequestAsync(
            request.RequestId,
            request.ReviewerId,
            request.Comments,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to revoke an active JIT elevation
/// </summary>
public sealed record RevokeJitElevationCommand(
    Guid RequestId,
    Guid RevokedBy,
    string Reason
) : ICommand<bool>;

public sealed class RevokeJitElevationValidator : AbstractValidator<RevokeJitElevationCommand>
{
    public RevokeJitElevationValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.RevokedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class RevokeJitElevationHandler(IJitElevationService service)
    : ICommandHandler<RevokeJitElevationCommand, bool>
{
    public async Task<bool> Handle(
        RevokeJitElevationCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.RevokeElevationAsync(
            request.RequestId,
            request.RevokedBy,
            request.Reason,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to cleanup expired JIT elevations
/// </summary>
public sealed record CleanupExpiredElevationsCommand : ICommand<int>;

public sealed class CleanupExpiredElevationsHandler(IJitElevationService service)
    : ICommandHandler<CleanupExpiredElevationsCommand, int>
{
    public async Task<int> Handle(
        CleanupExpiredElevationsCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.CleanupExpiredElevationsAsync(cancellationToken).ConfigureAwait(false);
    }
}
