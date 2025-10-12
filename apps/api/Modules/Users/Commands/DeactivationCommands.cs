using GameGuild.Modules.Users.Services;
using GameGuild.Modules.Users.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Commands;

// ============================================================================
// COMMANDS
// ============================================================================

/// <summary>
/// Command to request account deactivation with a grace period.
/// </summary>
public record RequestAccountDeactivationCommand(
    Guid UserId,
    string? Reason,
    string? Feedback,
    int GracePeriodDays = 30,
    string? IpAddress = null,
    string? UserAgent = null,
    string? Metadata = null) : IRequest<Result<AccountDeactivationRequestDto>>;

/// <summary>
/// Command to cancel a pending account deactivation.
/// </summary>
public record CancelAccountDeactivationCommand(Guid UserId) : IRequest<Result>;

/// <summary>
/// Command to process all due account deactivations (background job).
/// </summary>
public record ProcessDueDeactivationsCommand : IRequest<Result<int>>;

/// <summary>
/// Command to send deactivation reminders.
/// </summary>
public record SendDeactivationRemindersCommand(int DaysBeforeDeletion = 7) : IRequest<Result<int>>;

// ============================================================================
// QUERIES
// ============================================================================

/// <summary>
/// Query to get pending deactivation for a user.
/// </summary>
public record GetPendingDeactivationQuery(Guid UserId) : IRequest<Result<AccountDeactivationRequestDto?>>;

/// <summary>
/// Query to get all pending deactivations.
/// </summary>
public record GetAllPendingDeactivationsQuery : IRequest<Result<List<AccountDeactivationRequestDto>>>;

// ============================================================================
// DTO
// ============================================================================

/// <summary>
/// Data transfer object for account deactivation request.
/// </summary>
public record AccountDeactivationRequestDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? Reason { get; init; }
    public string? Feedback { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ScheduledDeletionAt { get; init; }
    public DeactivationStatus Status { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int RemindersSent { get; init; }
    public DateTime? LastReminderSentAt { get; init; }
    public bool IsPending { get; init; }
    public bool IsDue { get; init; }
    public int? DaysRemaining { get; init; }

    public static AccountDeactivationRequestDto FromEntity(AccountDeactivationRequest request)
    {
        return new AccountDeactivationRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            Reason = request.Reason,
            Feedback = request.Feedback,
            RequestedAt = request.RequestedAt,
            ScheduledDeletionAt = request.ScheduledDeletionAt,
            Status = request.Status,
            CancelledAt = request.CancelledAt,
            CompletedAt = request.CompletedAt,
            RemindersSent = request.RemindersSent,
            LastReminderSentAt = request.LastReminderSentAt,
            IsPending = request.IsPending,
            IsDue = request.IsDue,
            DaysRemaining = request.DaysRemaining
        };
    }
}

// ============================================================================
// HANDLERS
// ============================================================================

/// <summary>
/// Handler for requesting account deactivation.
/// </summary>
public class RequestAccountDeactivationHandler
    : IRequestHandler<RequestAccountDeactivationCommand, Result<AccountDeactivationRequestDto>>
{
    private readonly IDeactivationService _deactivationService;

    public RequestAccountDeactivationHandler(IDeactivationService deactivationService)
    {
        _deactivationService = deactivationService;
    }

    public async Task<Result<AccountDeactivationRequestDto>> Handle(
        RequestAccountDeactivationCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _deactivationService.RequestDeactivationAsync(
            request.UserId,
            request.Reason,
            request.Feedback,
            request.GracePeriodDays,
            request.IpAddress,
            request.UserAgent,
            request.Metadata);

        if (!result.IsSuccess)
        {
            return Result<AccountDeactivationRequestDto>.Failure(result.Error!);
        }

        return Result<AccountDeactivationRequestDto>.Success(
            AccountDeactivationRequestDto.FromEntity(result.Data!));
    }
}

/// <summary>
/// Handler for cancelling account deactivation.
/// </summary>
public class CancelAccountDeactivationHandler : IRequestHandler<CancelAccountDeactivationCommand, Result>
{
    private readonly IDeactivationService _deactivationService;

    public CancelAccountDeactivationHandler(IDeactivationService deactivationService)
    {
        _deactivationService = deactivationService;
    }

    public async Task<Result> Handle(
        CancelAccountDeactivationCommand request,
        CancellationToken cancellationToken)
    {
        return await _deactivationService.CancelDeactivationAsync(request.UserId);
    }
}

/// <summary>
/// Handler for processing due deactivations.
/// </summary>
public class ProcessDueDeactivationsHandler : IRequestHandler<ProcessDueDeactivationsCommand, Result<int>>
{
    private readonly IDeactivationService _deactivationService;

    public ProcessDueDeactivationsHandler(IDeactivationService deactivationService)
    {
        _deactivationService = deactivationService;
    }

    public async Task<Result<int>> Handle(
        ProcessDueDeactivationsCommand request,
        CancellationToken cancellationToken)
    {
        return await _deactivationService.ProcessDueDeactivationsAsync();
    }
}

/// <summary>
/// Handler for sending deactivation reminders.
/// </summary>
public class SendDeactivationRemindersHandler : IRequestHandler<SendDeactivationRemindersCommand, Result<int>>
{
    private readonly IDeactivationService _deactivationService;

    public SendDeactivationRemindersHandler(IDeactivationService deactivationService)
    {
        _deactivationService = deactivationService;
    }

    public async Task<Result<int>> Handle(
        SendDeactivationRemindersCommand request,
        CancellationToken cancellationToken)
    {
        return await _deactivationService.SendDeactivationRemindersAsync(request.DaysBeforeDeletion);
    }
}

/// <summary>
/// Handler for getting pending deactivation.
/// </summary>
public class GetPendingDeactivationHandler
    : IRequestHandler<GetPendingDeactivationQuery, Result<AccountDeactivationRequestDto?>>
{
    private readonly IDeactivationService _deactivationService;

    public GetPendingDeactivationHandler(IDeactivationService deactivationService)
    {
        _deactivationService = deactivationService;
    }

    public async Task<Result<AccountDeactivationRequestDto?>> Handle(
        GetPendingDeactivationQuery request,
        CancellationToken cancellationToken)
    {
        var deactivation = await _deactivationService.GetPendingDeactivationAsync(request.UserId);

        var dto = deactivation != null ? AccountDeactivationRequestDto.FromEntity(deactivation) : null;

        return Result<AccountDeactivationRequestDto?>.Success(dto);
    }
}

/// <summary>
/// Handler for getting all pending deactivations.
/// </summary>
public class GetAllPendingDeactivationsHandler
    : IRequestHandler<GetAllPendingDeactivationsQuery, Result<List<AccountDeactivationRequestDto>>>
{
    private readonly IDeactivationService _deactivationService;

    public GetAllPendingDeactivationsHandler(IDeactivationService deactivationService)
    {
        _deactivationService = deactivationService;
    }

    public async Task<Result<List<AccountDeactivationRequestDto>>> Handle(
        GetAllPendingDeactivationsQuery request,
        CancellationToken cancellationToken)
    {
        var deactivations = await _deactivationService.GetAllPendingDeactivationsAsync();

        var dtos = deactivations.Select(AccountDeactivationRequestDto.FromEntity).ToList();

        return Result<List<AccountDeactivationRequestDto>>.Success(dtos);
    }
}
