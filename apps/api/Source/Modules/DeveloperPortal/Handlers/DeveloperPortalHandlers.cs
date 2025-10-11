using GameGuild.CQRS;
using GameGuild.Modules.DeveloperPortal.Commands;
using GameGuild.Modules.DeveloperPortal.Queries;
using GameGuild.Modules.DeveloperPortal.Entities;
using GameGuild.Modules.DeveloperPortal.Services;

namespace GameGuild.Modules.DeveloperPortal.Handlers;

// Command Handlers
public class GenerateApiKeyHandler : IRequestHandler<GenerateApiKeyCommand, Result<Guid>>
{
    private readonly IDeveloperPortalService _service;

    public GenerateApiKeyHandler(IDeveloperPortalService service)
    {
        _service = service;
    }

    public async Task<Result<Guid>> Handle(GenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var apiKey = await _service.GenerateApiKeyAsync(
            request.DeveloperId,
            request.Name,
            request.TenantId,
            request.Scopes,
            request.ExpiresAt,
            cancellationToken);

        return Result<Guid>.Success(apiKey.Id);
    }
}

public class RevokeApiKeyHandler : IRequestHandler<RevokeApiKeyCommand, Result<Unit>>
{
    private readonly IDeveloperPortalService _service;

    public RevokeApiKeyHandler(IDeveloperPortalService service)
    {
        _service = service;
    }

    public async Task<Result<Unit>> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var revoked = await _service.RevokeApiKeyAsync(request.ApiKeyId, cancellationToken);

        return revoked
            ? Result<Unit>.Success(Unit.Value)
            : Result<Unit>.Failure("API key not found");
    }
}

public class RotateApiKeyHandler : IRequestHandler<RotateApiKeyCommand, Result<Guid>>
{
    private readonly IDeveloperPortalService _service;

    public RotateApiKeyHandler(IDeveloperPortalService service)
    {
        _service = service;
    }

    public async Task<Result<Guid>> Handle(RotateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var newKey = await _service.RotateApiKeyAsync(request.ApiKeyId, cancellationToken);

        return Result<Guid>.Success(newKey.Id);
    }
}

public class LogApiUsageHandler : IRequestHandler<LogApiUsageCommand, Result<Unit>>
{
    private readonly IDeveloperPortalService _service;

    public LogApiUsageHandler(IDeveloperPortalService service)
    {
        _service = service;
    }

    public async Task<Result<Unit>> Handle(LogApiUsageCommand request, CancellationToken cancellationToken)
    {
        await _service.LogApiUsageAsync(
            request.ApiKeyId,
            request.Endpoint,
            request.Method,
            request.StatusCode,
            request.ResponseTimeMs,
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

public class StartOnboardingHandler : IRequestHandler<StartOnboardingCommand, Result<Guid>>
{
    private readonly IOnboardingService _service;

    public StartOnboardingHandler(IOnboardingService service)
    {
        _service = service;
    }

    public async Task<Result<Guid>> Handle(StartOnboardingCommand request, CancellationToken cancellationToken)
    {
        var onboarding = await _service.StartOnboardingAsync(
            request.DeveloperId,
            request.TenantId,
            cancellationToken);

        return Result<Guid>.Success(onboarding.Id);
    }
}

public class CompleteOnboardingHandler : IRequestHandler<CompleteOnboardingCommand, Result<Unit>>
{
    private readonly IOnboardingService _service;

    public CompleteOnboardingHandler(IOnboardingService service)
    {
        _service = service;
    }

    public async Task<Result<Unit>> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        await _service.CompleteOnboardingAsync(request.DeveloperId, cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

public class UpdateOnboardingProgressHandler : IRequestHandler<UpdateOnboardingProgressCommand, Result<Unit>>
{
    private readonly IOnboardingService _service;

    public UpdateOnboardingProgressHandler(IOnboardingService service)
    {
        _service = service;
    }

    public async Task<Result<Unit>> Handle(UpdateOnboardingProgressCommand request, CancellationToken cancellationToken)
    {
        await _service.UpdateOnboardingProgressAsync(
            request.DeveloperId,
            request.StepKey,
            request.Completed,
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

// Query Handlers
public class GetApiKeysByDeveloperHandler : IRequestHandler<GetApiKeysByDeveloperQuery, Result<List<ApiKey>>>
{
    private readonly IDeveloperPortalService _service;

    public GetApiKeysByDeveloperHandler(IDeveloperPortalService service)
    {
        _service = service;
    }

    public async Task<Result<List<ApiKey>>> Handle(GetApiKeysByDeveloperQuery request, CancellationToken cancellationToken)
    {
        var apiKeys = await _service.GetApiKeysByDeveloperAsync(
            request.DeveloperId,
            request.IncludeRevoked,
            cancellationToken);

        return Result<List<ApiKey>>.Success(apiKeys);
    }
}

public class GetApiUsageStatsHandler : IRequestHandler<GetApiUsageStatsQuery, Result<ApiUsageStatsDto>>
{
    private readonly IDeveloperPortalService _service;

    public GetApiUsageStatsHandler(IDeveloperPortalService service)
    {
        _service = service;
    }

    public async Task<Result<ApiUsageStatsDto>> Handle(GetApiUsageStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _service.GetApiUsageStatsAsync(
            request.DeveloperId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return Result<ApiUsageStatsDto>.Success(stats);
    }
}

public class GetApiUsageLogsHandler : IRequestHandler<GetApiUsageLogsQuery, Result<List<ApiUsageLog>>>
{
    private readonly IDeveloperPortalService _service;

    public GetApiUsageLogsHandler(IDeveloperPortalService service)
    {
        _service = service;
    }

    public async Task<Result<List<ApiUsageLog>>> Handle(GetApiUsageLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _service.GetApiUsageLogsAsync(
            request.DeveloperId,
            request.StartDate,
            request.EndDate,
            request.Skip,
            request.Take,
            cancellationToken);

        return Result<List<ApiUsageLog>>.Success(logs);
    }
}

public class GetOnboardingStatusHandler : IRequestHandler<GetOnboardingStatusQuery, Result<DeveloperOnboarding?>>
{
    private readonly IOnboardingService _service;

    public GetOnboardingStatusHandler(IOnboardingService service)
    {
        _service = service;
    }

    public async Task<Result<DeveloperOnboarding?>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var onboarding = await _service.GetOnboardingStatusAsync(request.DeveloperId, cancellationToken);

        return Result<DeveloperOnboarding?>.Success(onboarding);
    }
}
