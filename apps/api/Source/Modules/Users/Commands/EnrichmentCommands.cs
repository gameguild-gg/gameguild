using GameGuild.Core.Helpers;
using GameGuild.Modules.Users.Entities;
using GameGuild.Modules.Users.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Commands;

// ======================== COMMANDS ========================

/// <summary>
/// Command to ingest a user behavior event.
/// </summary>
public record IngestEventCommand(
    Guid UserId,
    string EventType,
    Dictionary<string, object> Properties,
    string? SessionId = null,
    string? Source = null) : IRequest<Result<Guid>>;

/// <summary>
/// Command to batch ingest multiple events.
/// </summary>
public record BatchIngestEventsCommand(
    List<UserBehaviorEvent> Events) : IRequest<Result<int>>;

/// <summary>
/// Command to extract profile attributes from events.
/// </summary>
public record ExtractAttributesCommand(Guid UserId) : IRequest<Result<List<ProfileAttributeDto>>>;

/// <summary>
/// Command to update a profile attribute.
/// </summary>
public record UpdateAttributeCommand(
    Guid AttributeId,
    string NewValue,
    double Confidence,
    string? Metadata = null) : IRequest<Result>;

/// <summary>
/// Command to delete a profile attribute.
/// </summary>
public record DeleteAttributeCommand(Guid AttributeId) : IRequest<Result>;

/// <summary>
/// Command to purge expired events (background job).
/// </summary>
public record PurgeExpiredEventsCommand() : IRequest<Result<int>>;

// ======================== QUERIES ========================

/// <summary>
/// Query to get all profile attributes for a user.
/// </summary>
public record GetUserAttributesQuery(
    Guid UserId,
    bool ActiveOnly = true) : IRequest<Result<List<ProfileAttributeDto>>>;

/// <summary>
/// Query to get event history for a user.
/// </summary>
public record GetEventHistoryQuery(
    Guid UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Limit = 100) : IRequest<Result<List<BehaviorEventDto>>>;

/// <summary>
/// Query to get attributes by source.
/// </summary>
public record GetAttributesBySourceQuery(
    Guid UserId,
    string Source) : IRequest<Result<List<ProfileAttributeDto>>>;

/// <summary>
/// Query to calculate attribute confidence.
/// </summary>
public record CalculateConfidenceQuery(
    Guid UserId,
    string AttributeKey) : IRequest<Result<double>>;

// ======================== DTOs ========================

/// <summary>
/// DTO for profile attribute.
/// </summary>
public class ProfileAttributeDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AttributeKey { get; set; } = string.Empty;
    public string AttributeValue { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RecalculationCount { get; set; }
    public bool IsHighConfidence { get; set; }
    public bool IsExpired { get; set; }
}

/// <summary>
/// DTO for behavior event.
/// </summary>
public class BehaviorEventDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Properties { get; set; } = "{}";
    public string? SessionId { get; set; }
    public string? Source { get; set; }
    public string? Page { get; set; }
    public bool IsProcessed { get; set; }
}

// ======================== HANDLERS ========================

public class IngestEventHandler : IRequestHandler<IngestEventCommand, Result<Guid>>
{
    private readonly IEnrichmentService _service;

    public IngestEventHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<Guid>> Handle(
        IngestEventCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return Result<Guid>.Failure("Event type cannot be empty");
        }

        var result = await _service.IngestEventAsync(
            request.UserId,
            request.EventType,
            request.Properties,
            request.SessionId,
            request.Source);

        return result.IsSuccess
            ? Result<Guid>.Success(result.Data!.Id)
            : Result<Guid>.Failure(result.Error!);
    }
}

public class BatchIngestEventsHandler : IRequestHandler<BatchIngestEventsCommand, Result<int>>
{
    private readonly IEnrichmentService _service;

    public BatchIngestEventsHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<int>> Handle(
        BatchIngestEventsCommand request,
        CancellationToken cancellationToken)
    {
        return await _service.BatchIngestEventsAsync(request.Events);
    }
}

public class ExtractAttributesHandler : IRequestHandler<ExtractAttributesCommand, Result<List<ProfileAttributeDto>>>
{
    private readonly IEnrichmentService _service;

    public ExtractAttributesHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<List<ProfileAttributeDto>>> Handle(
        ExtractAttributesCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ExtractAttributesAsync(request.UserId);

        if (!result.IsSuccess)
        {
            return Result<List<ProfileAttributeDto>>.Failure(result.Error!);
        }

        var dtos = result.Data!.Select(a => new ProfileAttributeDto
        {
            Id = a.Id,
            UserId = a.UserId,
            AttributeKey = a.AttributeKey,
            AttributeValue = a.AttributeValue,
            Source = a.Source,
            Confidence = a.Confidence,
            UpdatedAt = a.UpdatedAt,
            ExpiresAt = a.ExpiresAt,
            RecalculationCount = a.RecalculationCount,
            IsHighConfidence = a.IsHighConfidence,
            IsExpired = a.IsExpired
        }).ToList();

        return Result<List<ProfileAttributeDto>>.Success(dtos);
    }
}

public class UpdateAttributeHandler : IRequestHandler<UpdateAttributeCommand, Result>
{
    private readonly IEnrichmentService _service;

    public UpdateAttributeHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(
        UpdateAttributeCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewValue))
        {
            return Result.Failure("New value cannot be empty");
        }

        if (request.Confidence < 0 || request.Confidence > 1)
        {
            return Result.Failure("Confidence must be between 0 and 1");
        }

        return await _service.UpdateAttributeAsync(
            request.AttributeId,
            request.NewValue,
            request.Confidence,
            request.Metadata);
    }
}

public class DeleteAttributeHandler : IRequestHandler<DeleteAttributeCommand, Result>
{
    private readonly IEnrichmentService _service;

    public DeleteAttributeHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(
        DeleteAttributeCommand request,
        CancellationToken cancellationToken)
    {
        return await _service.DeleteAttributeAsync(request.AttributeId);
    }
}

public class PurgeExpiredEventsHandler : IRequestHandler<PurgeExpiredEventsCommand, Result<int>>
{
    private readonly IEnrichmentService _service;

    public PurgeExpiredEventsHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<int>> Handle(
        PurgeExpiredEventsCommand request,
        CancellationToken cancellationToken)
    {
        return await _service.PurgeExpiredEventsAsync();
    }
}

public class GetUserAttributesHandler : IRequestHandler<GetUserAttributesQuery, Result<List<ProfileAttributeDto>>>
{
    private readonly IEnrichmentService _service;

    public GetUserAttributesHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<List<ProfileAttributeDto>>> Handle(
        GetUserAttributesQuery request,
        CancellationToken cancellationToken)
    {
        var attributes = await _service.GetUserAttributesAsync(request.UserId, request.ActiveOnly);

        var dtos = attributes.Select(a => new ProfileAttributeDto
        {
            Id = a.Id,
            UserId = a.UserId,
            AttributeKey = a.AttributeKey,
            AttributeValue = a.AttributeValue,
            Source = a.Source,
            Confidence = a.Confidence,
            UpdatedAt = a.UpdatedAt,
            ExpiresAt = a.ExpiresAt,
            RecalculationCount = a.RecalculationCount,
            IsHighConfidence = a.IsHighConfidence,
            IsExpired = a.IsExpired
        }).ToList();

        return Result<List<ProfileAttributeDto>>.Success(dtos);
    }
}

public class GetEventHistoryHandler : IRequestHandler<GetEventHistoryQuery, Result<List<BehaviorEventDto>>>
{
    private readonly IEnrichmentService _service;

    public GetEventHistoryHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<List<BehaviorEventDto>>> Handle(
        GetEventHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var events = await _service.GetEventHistoryAsync(
            request.UserId,
            request.StartDate,
            request.EndDate,
            request.Limit);

        var dtos = events.Select(e => new BehaviorEventDto
        {
            Id = e.Id,
            UserId = e.UserId,
            EventType = e.EventType,
            Timestamp = e.Timestamp,
            Properties = e.Properties,
            SessionId = e.SessionId,
            Source = e.Source,
            Page = e.Page,
            IsProcessed = e.IsProcessed
        }).ToList();

        return Result<List<BehaviorEventDto>>.Success(dtos);
    }
}

public class GetAttributesBySourceHandler : IRequestHandler<GetAttributesBySourceQuery, Result<List<ProfileAttributeDto>>>
{
    private readonly IEnrichmentService _service;

    public GetAttributesBySourceHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<List<ProfileAttributeDto>>> Handle(
        GetAttributesBySourceQuery request,
        CancellationToken cancellationToken)
    {
        var attributes = await _service.GetAttributesBySourceAsync(request.UserId, request.Source);

        var dtos = attributes.Select(a => new ProfileAttributeDto
        {
            Id = a.Id,
            UserId = a.UserId,
            AttributeKey = a.AttributeKey,
            AttributeValue = a.AttributeValue,
            Source = a.Source,
            Confidence = a.Confidence,
            UpdatedAt = a.UpdatedAt,
            ExpiresAt = a.ExpiresAt,
            RecalculationCount = a.RecalculationCount,
            IsHighConfidence = a.IsHighConfidence,
            IsExpired = a.IsExpired
        }).ToList();

        return Result<List<ProfileAttributeDto>>.Success(dtos);
    }
}

public class CalculateConfidenceHandler : IRequestHandler<CalculateConfidenceQuery, Result<double>>
{
    private readonly IEnrichmentService _service;

    public CalculateConfidenceHandler(IEnrichmentService service)
    {
        _service = service;
    }

    public async Task<Result<double>> Handle(
        CalculateConfidenceQuery request,
        CancellationToken cancellationToken)
    {
        var confidence = await _service.CalculateAttributeConfidenceAsync(
            request.UserId,
            request.AttributeKey);

        return Result<double>.Success(confidence);
    }
}
