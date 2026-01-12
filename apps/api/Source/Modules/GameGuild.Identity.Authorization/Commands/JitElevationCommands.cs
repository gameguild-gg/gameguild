using FluentValidation;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Commands;

// ============================================================================
// JIT Elevation Commands
// ============================================================================

/// <summary>
///     Command to request a Just-in-Time permission elevation
/// </summary>
public record RequestJitElevationCommand(
    Guid RequesterId,
    Guid? TenantId,
    string Permission,
    string Justification,
    int DurationMinutes,
    Guid? ResourceId = null,
    string? ResourceType = null,
    DateTime? StartsAt = null
) : ICommand<JitElevationRequest>;

public class RequestJitElevationValidator : AbstractValidator<RequestJitElevationCommand>
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

public class RequestJitElevationHandler(IJitElevationService service)
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
public record ApproveJitElevationCommand(
    Guid RequestId,
    Guid ReviewerId,
    string? Comments = null
) : ICommand<JitElevationRequest>;

public class ApproveJitElevationValidator : AbstractValidator<ApproveJitElevationCommand>
{
    public ApproveJitElevationValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments != null);
    }
}

public class ApproveJitElevationHandler(IJitElevationService service)
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
        );
    }
}

/// <summary>
///     Command to deny a JIT elevation request
/// </summary>
public record DenyJitElevationCommand(
    Guid RequestId,
    Guid ReviewerId,
    string Comments
) : ICommand<JitElevationRequest>;

public class DenyJitElevationValidator : AbstractValidator<DenyJitElevationCommand>
{
    public DenyJitElevationValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(2000);
    }
}

public class DenyJitElevationHandler(IJitElevationService service)
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
        );
    }
}

/// <summary>
///     Command to revoke an active JIT elevation
/// </summary>
public record RevokeJitElevationCommand(
    Guid RequestId,
    Guid RevokedBy,
    string Reason
) : ICommand<bool>;

public class RevokeJitElevationValidator : AbstractValidator<RevokeJitElevationCommand>
{
    public RevokeJitElevationValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.RevokedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public class RevokeJitElevationHandler(IJitElevationService service)
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
        );
    }
}

/// <summary>
///     Command to cleanup expired JIT elevations
/// </summary>
public record CleanupExpiredElevationsCommand : ICommand<int>;

public class CleanupExpiredElevationsHandler(IJitElevationService service)
    : ICommandHandler<CleanupExpiredElevationsCommand, int>
{
    public async Task<int> Handle(
        CleanupExpiredElevationsCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.CleanupExpiredElevationsAsync(cancellationToken);
    }
}
