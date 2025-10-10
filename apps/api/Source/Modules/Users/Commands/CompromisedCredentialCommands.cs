using GameGuild.Core.Helpers;
using GameGuild.Modules.Users.Entities;
using GameGuild.Modules.Users.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Commands;

// ======================== COMMANDS ========================

/// <summary>
/// Command to check if a credential has been compromised.
/// </summary>
public record CheckCredentialCommand(
    Guid UserId,
    string Credential,
    string? IpAddress = null) : IRequest<Result<CredentialCheckResult>>;

/// <summary>
/// Command to acknowledge a compromised credential.
/// </summary>
public record AcknowledgeCompromiseCommand(Guid CompromiseId) : IRequest<Result>;

/// <summary>
/// Command to resolve a compromised credential.
/// </summary>
public record ResolveCompromiseCommand(
    Guid CompromiseId,
    string ResolutionAction) : IRequest<Result>;

/// <summary>
/// Command to ignore a compromised credential.
/// </summary>
public record IgnoreCompromiseCommand(Guid CompromiseId) : IRequest<Result>;

/// <summary>
/// Command to scan all users for compromised credentials (admin only).
/// </summary>
public record ScanAllUsersCommand() : IRequest<Result<int>>;

// ======================== QUERIES ========================

/// <summary>
/// Query to get all compromised credentials for a user.
/// </summary>
public record GetUserCompromisedCredentialsQuery(
    Guid UserId,
    bool ActiveOnly = true) : IRequest<Result<List<CompromisedCredentialDto>>>;

/// <summary>
/// Query to get compromise statistics for a user.
/// </summary>
public record GetUserCompromiseStatisticsQuery(Guid UserId) : IRequest<Result<CompromiseStatistics>>;

// ======================== DTOs ========================

/// <summary>
/// DTO for compromised credential information.
/// </summary>
public class CompromisedCredentialDto
{
    public Guid Id { get; set; }
    public string CredentialType { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? BreachName { get; set; }
    public DateTime? BreachDate { get; set; }
    public BreachSeverity Severity { get; set; }
    public int BreachCount { get; set; }
    public CompromiseStatus Status { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionAction { get; set; }
    public bool RequiresAction { get; set; }
}

// ======================== HANDLERS ========================

public class CheckCredentialHandler : IRequestHandler<CheckCredentialCommand, Result<CredentialCheckResult>>
{
    private readonly ICompromisedCredentialService _service;

    public CheckCredentialHandler(ICompromisedCredentialService service)
    {
        _service = service;
    }

    public async Task<Result<CredentialCheckResult>> Handle(
        CheckCredentialCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Credential))
        {
            return Result<CredentialCheckResult>.Failure("Credential cannot be empty");
        }

        return await _service.CheckUserCredentialAsync(
            request.UserId,
            request.Credential,
            request.IpAddress);
    }
}

public class AcknowledgeCompromiseHandler : IRequestHandler<AcknowledgeCompromiseCommand, Result>
{
    private readonly ICompromisedCredentialService _service;

    public AcknowledgeCompromiseHandler(ICompromisedCredentialService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(
        AcknowledgeCompromiseCommand request,
        CancellationToken cancellationToken)
    {
        return await _service.AcknowledgeCompromiseAsync(request.CompromiseId);
    }
}

public class ResolveCompromiseHandler : IRequestHandler<ResolveCompromiseCommand, Result>
{
    private readonly ICompromisedCredentialService _service;

    public ResolveCompromiseHandler(ICompromisedCredentialService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(
        ResolveCompromiseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResolutionAction))
        {
            return Result.Failure("Resolution action cannot be empty");
        }

        return await _service.ResolveCompromiseAsync(request.CompromiseId, request.ResolutionAction);
    }
}

public class IgnoreCompromiseHandler : IRequestHandler<IgnoreCompromiseCommand, Result>
{
    private readonly ICompromisedCredentialService _service;

    public IgnoreCompromiseHandler(ICompromisedCredentialService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(
        IgnoreCompromiseCommand request,
        CancellationToken cancellationToken)
    {
        return await _service.IgnoreCompromiseAsync(request.CompromiseId);
    }
}

public class ScanAllUsersHandler : IRequestHandler<ScanAllUsersCommand, Result<int>>
{
    private readonly ICompromisedCredentialService _service;

    public ScanAllUsersHandler(ICompromisedCredentialService service)
    {
        _service = service;
    }

    public async Task<Result<int>> Handle(
        ScanAllUsersCommand request,
        CancellationToken cancellationToken)
    {
        return await _service.ScanAllUsersAsync();
    }
}

public class GetUserCompromisedCredentialsHandler
    : IRequestHandler<GetUserCompromisedCredentialsQuery, Result<List<CompromisedCredentialDto>>>
{
    private readonly ICompromisedCredentialService _service;

    public GetUserCompromisedCredentialsHandler(ICompromisedCredentialService service)
    {
        _service = service;
    }

    public async Task<Result<List<CompromisedCredentialDto>>> Handle(
        GetUserCompromisedCredentialsQuery request,
        CancellationToken cancellationToken)
    {
        var compromises = await _service.GetUserCompromisedCredentialsAsync(
            request.UserId,
            request.ActiveOnly);

        var dtos = compromises.Select(c => new CompromisedCredentialDto
        {
            Id = c.Id,
            CredentialType = c.CredentialType,
            DetectedAt = c.DetectedAt,
            Source = c.Source,
            BreachName = c.BreachName,
            BreachDate = c.BreachDate,
            Severity = c.Severity,
            BreachCount = c.BreachCount,
            Status = c.Status,
            NotifiedAt = c.NotifiedAt,
            AcknowledgedAt = c.AcknowledgedAt,
            ResolvedAt = c.ResolvedAt,
            ResolutionAction = c.ResolutionAction,
            RequiresAction = c.RequiresAction
        }).ToList();

        return Result<List<CompromisedCredentialDto>>.Success(dtos);
    }
}

public class GetUserCompromiseStatisticsHandler
    : IRequestHandler<GetUserCompromiseStatisticsQuery, Result<CompromiseStatistics>>
{
    private readonly ICompromisedCredentialService _service;

    public GetUserCompromiseStatisticsHandler(ICompromisedCredentialService service)
    {
        _service = service;
    }

    public async Task<Result<CompromiseStatistics>> Handle(
        GetUserCompromiseStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var statistics = await _service.GetUserStatisticsAsync(request.UserId);
        return Result<CompromiseStatistics>.Success(statistics);
    }
}
