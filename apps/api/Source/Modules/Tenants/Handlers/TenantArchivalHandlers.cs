using GameGuild.CQRS;
using GameGuild.Modules.Tenants.Commands;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Tenants.Repositories;
using GameGuild.Modules.Tenants.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants.Handlers;

// Create Policy Handler
public class CreateTenantArchivalPolicyHandler : IRequestHandler<CreateTenantArchivalPolicyCommand, Result<TenantArchivalPolicy>>
{
    private readonly ITenantArchivalService _archivalService;
    private readonly ILogger<CreateTenantArchivalPolicyHandler> _logger;

    public CreateTenantArchivalPolicyHandler(ITenantArchivalService archivalService, ILogger<CreateTenantArchivalPolicyHandler> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task<Result<TenantArchivalPolicy>> Handle(CreateTenantArchivalPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _archivalService.CreatePolicyAsync(
                request.TenantId, request.PolicyName, request.InactivityThresholdDays,
                request.WarningDaysBeforeArchival, request.AutoPurgeAfterDays, request.NotificationEmails);

            return Result<TenantArchivalPolicy>.Success(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating archival policy for tenant {TenantId}", request.TenantId);
            return Result<TenantArchivalPolicy>.Failure($"Error creating archival policy: {ex.Message}");
        }
    }
}

// Update Policy Handler
public class UpdateTenantArchivalPolicyHandler : IRequestHandler<UpdateTenantArchivalPolicyCommand, Result<TenantArchivalPolicy>>
{
    private readonly ITenantArchivalService _archivalService;
    private readonly ILogger<UpdateTenantArchivalPolicyHandler> _logger;

    public UpdateTenantArchivalPolicyHandler(ITenantArchivalService archivalService, ILogger<UpdateTenantArchivalPolicyHandler> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task<Result<TenantArchivalPolicy>> Handle(UpdateTenantArchivalPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _archivalService.UpdatePolicyAsync(
                request.PolicyId, request.IsEnabled, request.InactivityThresholdDays,
                request.WarningDaysBeforeArchival, request.AutoPurgeAfterDays);

            return Result<TenantArchivalPolicy>.Success(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating archival policy {PolicyId}", request.PolicyId);
            return Result<TenantArchivalPolicy>.Failure($"Error updating archival policy: {ex.Message}");
        }
    }
}

// Archive Tenant Handler
public class ArchiveTenantHandler : IRequestHandler<ArchiveTenantCommand, Result<TenantArchiveRecord>>
{
    private readonly ITenantArchivalService _archivalService;
    private readonly ILogger<ArchiveTenantHandler> _logger;

    public ArchiveTenantHandler(ITenantArchivalService archivalService, ILogger<ArchiveTenantHandler> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task<Result<TenantArchiveRecord>> Handle(ArchiveTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var record = await _archivalService.ArchiveTenantAsync(
                request.TenantId, request.ArchivedBy, request.Reason);

            return Result<TenantArchiveRecord>.Success(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving tenant {TenantId}", request.TenantId);
            return Result<TenantArchiveRecord>.Failure($"Error archiving tenant: {ex.Message}");
        }
    }
}

// Restore Tenant Handler
public class RestoreTenantFromArchiveHandler : IRequestHandler<RestoreTenantFromArchiveCommand, Result<TenantArchiveRecord>>
{
    private readonly ITenantArchivalService _archivalService;
    private readonly ILogger<RestoreTenantFromArchiveHandler> _logger;

    public RestoreTenantFromArchiveHandler(ITenantArchivalService archivalService, ILogger<RestoreTenantFromArchiveHandler> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task<Result<TenantArchiveRecord>> Handle(RestoreTenantFromArchiveCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var record = await _archivalService.RestoreTenantAsync(
                request.ArchiveRecordId, request.RestoredBy);

            return Result<TenantArchiveRecord>.Success(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring tenant from archive {ArchiveRecordId}", request.ArchiveRecordId);
            return Result<TenantArchiveRecord>.Failure($"Error restoring tenant: {ex.Message}");
        }
    }
}

// Purge Tenant Handler
public class PurgeTenantHandler : IRequestHandler<PurgeTenantCommand, Result>
{
    private readonly ITenantArchivalService _archivalService;
    private readonly ILogger<PurgeTenantHandler> _logger;

    public PurgeTenantHandler(ITenantArchivalService archivalService, ILogger<PurgeTenantHandler> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task<Result> Handle(PurgeTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _archivalService.PurgeTenantAsync(request.ArchiveRecordId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging tenant {ArchiveRecordId}", request.ArchiveRecordId);
            return Result.Failure($"Error purging tenant: {ex.Message}");
        }
    }
}

// Detect Inactive Tenants Handler
public class DetectInactiveTenantsHandler : IRequestHandler<DetectInactiveTenantsQuery, Result<List<Guid>>>
{
    private readonly ITenantArchivalService _archivalService;
    private readonly ILogger<DetectInactiveTenantsHandler> _logger;

    public DetectInactiveTenantsHandler(ITenantArchivalService archivalService, ILogger<DetectInactiveTenantsHandler> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task<Result<List<Guid>>> Handle(DetectInactiveTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var inactiveTenants = await _archivalService.DetectInactiveTenantsAsync(cancellationToken);
            return Result<List<Guid>>.Success(inactiveTenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting inactive tenants");
            return Result<List<Guid>>.Failure($"Error detecting inactive tenants: {ex.Message}");
        }
    }
}

// Send Warning Handler
public class SendTenantArchivalWarningHandler : IRequestHandler<SendTenantArchivalWarningCommand, Result>
{
    private readonly ITenantArchivalService _archivalService;
    private readonly ILogger<SendTenantArchivalWarningHandler> _logger;

    public SendTenantArchivalWarningHandler(ITenantArchivalService archivalService, ILogger<SendTenantArchivalWarningHandler> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task<Result> Handle(SendTenantArchivalWarningCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _archivalService.SendArchivalWarningAsync(request.TenantId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending archival warning to tenant {TenantId}", request.TenantId);
            return Result.Failure($"Error sending archival warning: {ex.Message}");
        }
    }
}

// Get Policy Handler
public class GetTenantArchivalPolicyHandler : IRequestHandler<GetTenantArchivalPolicyQuery, Result<TenantArchivalPolicy>>
{
    private readonly ITenantArchivalRepository _repository;
    private readonly ILogger<GetTenantArchivalPolicyHandler> _logger;

    public GetTenantArchivalPolicyHandler(ITenantArchivalRepository repository, ILogger<GetTenantArchivalPolicyHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<TenantArchivalPolicy>> Handle(GetTenantArchivalPolicyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _repository.GetPolicyByTenantIdAsync(request.TenantId, cancellationToken);
            if (policy == null)
            {
                return Result<TenantArchivalPolicy>.Failure($"No archival policy found for tenant {request.TenantId}");
            }
            return Result<TenantArchivalPolicy>.Success(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archival policy for tenant {TenantId}", request.TenantId);
            return Result<TenantArchivalPolicy>.Failure($"Error getting archival policy: {ex.Message}");
        }
    }
}

// Get Archive Record Handler
public class GetTenantArchiveRecordHandler : IRequestHandler<GetTenantArchiveRecordQuery, Result<TenantArchiveRecord>>
{
    private readonly ITenantArchivalRepository _repository;
    private readonly ILogger<GetTenantArchiveRecordHandler> _logger;

    public GetTenantArchiveRecordHandler(ITenantArchivalRepository repository, ILogger<GetTenantArchiveRecordHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<TenantArchiveRecord>> Handle(GetTenantArchiveRecordQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var record = await _repository.GetArchiveRecordByTenantIdAsync(request.TenantId, cancellationToken);
            if (record == null)
            {
                return Result<TenantArchiveRecord>.Failure($"No archive record found for tenant {request.TenantId}");
            }
            return Result<TenantArchiveRecord>.Success(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive record for tenant {TenantId}", request.TenantId);
            return Result<TenantArchiveRecord>.Failure($"Error getting archive record: {ex.Message}");
        }
    }
}
