using GameGuild.Helpers;
using GameGuild.Modules.Users;
using GameGuild.Modules.Users.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Commands;

// ============================================================================
// COMMANDS
// ============================================================================

/// <summary>
/// Command to update communication preferences for a channel.
/// </summary>
public record UpdateCommunicationPreferenceCommand(
    Guid UserId,
    string Channel,
    bool? IsEnabled = null,
    string? Locale = null,
    string? Timezone = null,
    string? QuietHoursStart = null,
    string? QuietHoursEnd = null,
    string? PreferredDeliveryStart = null,
    string? PreferredDeliveryEnd = null,
    string? AllowedDeliveryDays = null,
    bool? RespectQuietHoursForUrgent = null,
    string? DigestFrequency = null,
    string? DigestDeliveryTime = null,
    int? PriorityThreshold = null) : IRequest<Result<CommunicationPreferenceDto>>;

/// <summary>
/// Command to schedule a communication.
/// </summary>
public record ScheduleCommunicationCommand(
    Guid UserId,
    string Channel,
    string Content,
    string? Subject = null,
    int Priority = 2,
    string? Metadata = null) : IRequest<Result<ScheduledCommunicationDto>>;

/// <summary>
/// Command to process due communications (background job).
/// </summary>
public record ProcessDueCommunicationsCommand : IRequest<Result<int>>;

/// <summary>
/// Command to cancel a scheduled communication.
/// </summary>
public record CancelScheduledCommunicationCommand(Guid CommunicationId) : IRequest<Result>;

// ============================================================================
// QUERIES
// ============================================================================

/// <summary>
/// Query to get all communication preferences for a user.
/// </summary>
public record GetUserCommunicationPreferencesQuery(Guid UserId) : IRequest<Result<List<CommunicationPreferenceDto>>>;

/// <summary>
/// Query to get communication preference for a specific channel.
/// </summary>
public record GetCommunicationPreferenceQuery(Guid UserId, string Channel) : IRequest<Result<CommunicationPreferenceDto>>;

/// <summary>
/// Query to calculate optimal delivery time.
/// </summary>
public record CalculateOptimalDeliveryTimeQuery(Guid UserId, string Channel, int Priority) : IRequest<Result<DateTime>>;

// ============================================================================
// DTOs
// ============================================================================

/// <summary>
/// Data transfer object for communication preference.
/// </summary>
public record CommunicationPreferenceDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public string? Locale { get; init; }
    public string? Timezone { get; init; }
    public string? QuietHoursStart { get; init; }
    public string? QuietHoursEnd { get; init; }
    public string? PreferredDeliveryStart { get; init; }
    public string? PreferredDeliveryEnd { get; init; }
    public string? AllowedDeliveryDays { get; init; }
    public bool RespectQuietHoursForUrgent { get; init; }
    public string? DigestFrequency { get; init; }
    public string? DigestDeliveryTime { get; init; }
    public int PriorityThreshold { get; init; }
    public DateTime? LastReviewedAt { get; init; }

    public static CommunicationPreferenceDto FromEntity(CommunicationPreference preference)
    {
        return new CommunicationPreferenceDto
        {
            Id = preference.Id,
            UserId = preference.UserId,
            Channel = preference.Channel,
            IsEnabled = preference.IsEnabled,
            Locale = preference.Locale,
            Timezone = preference.Timezone,
            QuietHoursStart = preference.QuietHoursStart,
            QuietHoursEnd = preference.QuietHoursEnd,
            PreferredDeliveryStart = preference.PreferredDeliveryStart,
            PreferredDeliveryEnd = preference.PreferredDeliveryEnd,
            AllowedDeliveryDays = preference.AllowedDeliveryDays,
            RespectQuietHoursForUrgent = preference.RespectQuietHoursForUrgent,
            DigestFrequency = preference.DigestFrequency,
            DigestDeliveryTime = preference.DigestDeliveryTime,
            PriorityThreshold = preference.PriorityThreshold,
            LastReviewedAt = preference.LastReviewedAt
        };
    }
}

/// <summary>
/// Data transfer object for scheduled communication.
/// </summary>
public record ScheduledCommunicationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public int Priority { get; init; }
    public DateTime QueuedAt { get; init; }
    public DateTime ScheduledDeliveryAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DeliveryStatus Status { get; init; }
    public int DeliveryAttempts { get; init; }
    public bool IsDue { get; init; }

    public static ScheduledCommunicationDto FromEntity(ScheduledCommunication comm)
    {
        return new ScheduledCommunicationDto
        {
            Id = comm.Id,
            UserId = comm.UserId,
            Channel = comm.Channel,
            Content = comm.Content,
            Subject = comm.Subject,
            Priority = comm.Priority,
            QueuedAt = comm.QueuedAt,
            ScheduledDeliveryAt = comm.ScheduledDeliveryAt,
            DeliveredAt = comm.DeliveredAt,
            Status = comm.Status,
            DeliveryAttempts = comm.DeliveryAttempts,
            IsDue = comm.IsDue
        };
    }
}

// ============================================================================
// HANDLERS
// ============================================================================

/// <summary>
/// Handler for updating communication preferences.
/// </summary>
public class UpdateCommunicationPreferenceHandler
    : IRequestHandler<UpdateCommunicationPreferenceCommand, Result<CommunicationPreferenceDto>>
{
    private readonly ICommunicationSchedulingService _schedulingService;

    public UpdateCommunicationPreferenceHandler(ICommunicationSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    public async Task<Result<CommunicationPreferenceDto>> Handle(
        UpdateCommunicationPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _schedulingService.UpdatePreferenceAsync(
            request.UserId,
            request.Channel,
            request.IsEnabled,
            request.Locale,
            request.Timezone,
            request.QuietHoursStart,
            request.QuietHoursEnd,
            request.PreferredDeliveryStart,
            request.PreferredDeliveryEnd,
            request.AllowedDeliveryDays,
            request.RespectQuietHoursForUrgent,
            request.DigestFrequency,
            request.DigestDeliveryTime,
            request.PriorityThreshold);

        if (!result.IsSuccess)
        {
            return Result<CommunicationPreferenceDto>.Failure(result.Error!);
        }

        return Result<CommunicationPreferenceDto>.Success(
            CommunicationPreferenceDto.FromEntity(result.Data!));
    }
}

/// <summary>
/// Handler for scheduling communication.
/// </summary>
public class ScheduleCommunicationHandler
    : IRequestHandler<ScheduleCommunicationCommand, Result<ScheduledCommunicationDto>>
{
    private readonly ICommunicationSchedulingService _schedulingService;

    public ScheduleCommunicationHandler(ICommunicationSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    public async Task<Result<ScheduledCommunicationDto>> Handle(
        ScheduleCommunicationCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _schedulingService.ScheduleCommunicationAsync(
            request.UserId,
            request.Channel,
            request.Content,
            request.Subject,
            request.Priority,
            request.Metadata);

        if (!result.IsSuccess)
        {
            return Result<ScheduledCommunicationDto>.Failure(result.Error!);
        }

        return Result<ScheduledCommunicationDto>.Success(
            ScheduledCommunicationDto.FromEntity(result.Data!));
    }
}

/// <summary>
/// Handler for processing due communications.
/// </summary>
public class ProcessDueCommunicationsHandler : IRequestHandler<ProcessDueCommunicationsCommand, Result<int>>
{
    private readonly ICommunicationSchedulingService _schedulingService;

    public ProcessDueCommunicationsHandler(ICommunicationSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    public async Task<Result<int>> Handle(
        ProcessDueCommunicationsCommand request,
        CancellationToken cancellationToken)
    {
        return await _schedulingService.ProcessDueCommunicationsAsync();
    }
}

/// <summary>
/// Handler for cancelling scheduled communication.
/// </summary>
public class CancelScheduledCommunicationHandler : IRequestHandler<CancelScheduledCommunicationCommand, Result>
{
    private readonly ICommunicationSchedulingService _schedulingService;

    public CancelScheduledCommunicationHandler(ICommunicationSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    public async Task<Result> Handle(
        CancelScheduledCommunicationCommand request,
        CancellationToken cancellationToken)
    {
        return await _schedulingService.CancelScheduledCommunicationAsync(request.CommunicationId);
    }
}

/// <summary>
/// Handler for getting user communication preferences.
/// </summary>
public class GetUserCommunicationPreferencesHandler
    : IRequestHandler<GetUserCommunicationPreferencesQuery, Result<List<CommunicationPreferenceDto>>>
{
    private readonly ICommunicationSchedulingService _schedulingService;

    public GetUserCommunicationPreferencesHandler(ICommunicationSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    public async Task<Result<List<CommunicationPreferenceDto>>> Handle(
        GetUserCommunicationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var preferences = await _schedulingService.GetUserPreferencesAsync(request.UserId);

        var dtos = preferences.Select(CommunicationPreferenceDto.FromEntity).ToList();

        return Result<List<CommunicationPreferenceDto>>.Success(dtos);
    }
}

/// <summary>
/// Handler for getting communication preference for a channel.
/// </summary>
public class GetCommunicationPreferenceHandler
    : IRequestHandler<GetCommunicationPreferenceQuery, Result<CommunicationPreferenceDto>>
{
    private readonly ICommunicationSchedulingService _schedulingService;

    public GetCommunicationPreferenceHandler(ICommunicationSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    public async Task<Result<CommunicationPreferenceDto>> Handle(
        GetCommunicationPreferenceQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _schedulingService.GetOrCreatePreferenceAsync(request.UserId, request.Channel);

        if (!result.IsSuccess)
        {
            return Result<CommunicationPreferenceDto>.Failure(result.Error!);
        }

        return Result<CommunicationPreferenceDto>.Success(
            CommunicationPreferenceDto.FromEntity(result.Data!));
    }
}

/// <summary>
/// Handler for calculating optimal delivery time.
/// </summary>
public class CalculateOptimalDeliveryTimeHandler
    : IRequestHandler<CalculateOptimalDeliveryTimeQuery, Result<DateTime>>
{
    private readonly ICommunicationSchedulingService _schedulingService;

    public CalculateOptimalDeliveryTimeHandler(ICommunicationSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    public async Task<Result<DateTime>> Handle(
        CalculateOptimalDeliveryTimeQuery request,
        CancellationToken cancellationToken)
    {
        var deliveryTime = await _schedulingService.CalculateOptimalDeliveryTimeAsync(
            request.UserId,
            request.Channel,
            request.Priority);

        return Result<DateTime>.Success(deliveryTime);
    }
}
