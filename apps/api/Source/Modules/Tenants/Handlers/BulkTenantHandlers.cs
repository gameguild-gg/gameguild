using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for bulk creating tenants
/// </summary>
public class BulkCreateTenantsHandler : IRequestHandler<BulkCreateTenantsCommand, IEnumerable<Tenant>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<BulkCreateTenantsHandler> _logger;

    public BulkCreateTenantsHandler(ITenantRepository tenantRepository, ILogger<BulkCreateTenantsHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Tenant>> Handle(BulkCreateTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenants = new List<Tenant>();

        foreach (var dto in request.Tenants)
        {
            var tenant = new Tenant
            {
                Name = dto.Name,
                Slug = dto.Slug,
                Description = dto.Description,
                AdminEmail = dto.AdminEmail,
                IsDefault = dto.IsDefault,
                IsActive = true
            };

            tenants.Add(tenant);
        }

        await _tenantRepository.BulkCreateAsync(tenants, cancellationToken);
        _logger.LogInformation("Bulk created {Count} tenants", tenants.Count);

        return tenants;
    }
}

/// <summary>
/// Handler for bulk updating tenants
/// </summary>
public class BulkUpdateTenantsHandler : IRequestHandler<BulkUpdateTenantsCommand, IEnumerable<Tenant>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<BulkUpdateTenantsHandler> _logger;

    public BulkUpdateTenantsHandler(ITenantRepository tenantRepository, ILogger<BulkUpdateTenantsHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Tenant>> Handle(BulkUpdateTenantsCommand request, CancellationToken cancellationToken)
    {
        var ids = request.Tenants.Select(t => t.Id).ToList();
        var existingTenants = await _tenantRepository.GetByIdsAsync(ids, cancellationToken);
        var tenantDict = existingTenants.ToDictionary(t => t.Id);

        foreach (var dto in request.Tenants)
        {
            if (tenantDict.TryGetValue(dto.Id, out var tenant))
            {
                if (dto.Name != null) tenant.Name = dto.Name;
                if (dto.Slug != null) tenant.Slug = dto.Slug;
                if (dto.Description != null) tenant.Description = dto.Description;
                if (dto.AdminEmail != null) tenant.AdminEmail = dto.AdminEmail;
                tenant.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _tenantRepository.BulkUpdateAsync(existingTenants, cancellationToken);
        _logger.LogInformation("Bulk updated {Count} tenants", existingTenants.Count());

        return existingTenants;
    }
}

/// <summary>
/// Handler for bulk deleting tenants
/// </summary>
public class BulkDeleteTenantsHandler : IRequestHandler<BulkDeleteTenantsCommand, int>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<BulkDeleteTenantsHandler> _logger;

    public BulkDeleteTenantsHandler(ITenantRepository tenantRepository, ILogger<BulkDeleteTenantsHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<int> Handle(BulkDeleteTenantsCommand request, CancellationToken cancellationToken)
    {
        int count;
        if (request.SoftDelete)
        {
            count = await _tenantRepository.BulkSoftDeleteAsync(request.TenantIds, cancellationToken);
            _logger.LogInformation("Bulk soft deleted {Count} tenants", count);
        }
        else
        {
            count = await _tenantRepository.BulkHardDeleteAsync(request.TenantIds, cancellationToken);
            _logger.LogInformation("Bulk hard deleted {Count} tenants", count);
        }

        return count;
    }
}

/// <summary>
/// Handler for bulk activating tenants
/// </summary>
public class BulkActivateTenantsHandler : IRequestHandler<BulkActivateTenantsCommand, int>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<BulkActivateTenantsHandler> _logger;

    public BulkActivateTenantsHandler(ITenantRepository tenantRepository, ILogger<BulkActivateTenantsHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<int> Handle(BulkActivateTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.GetByIdsAsync(request.TenantIds, cancellationToken);
        foreach (var tenant in tenants)
        {
            tenant.Activate();
        }

        await _tenantRepository.BulkUpdateAsync(tenants, cancellationToken);
        _logger.LogInformation("Bulk activated {Count} tenants", tenants.Count());

        return tenants.Count();
    }
}

/// <summary>
/// Handler for bulk deactivating tenants
/// </summary>
public class BulkDeactivateTenantsHandler : IRequestHandler<BulkDeactivateTenantsCommand, int>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<BulkDeactivateTenantsHandler> _logger;

    public BulkDeactivateTenantsHandler(ITenantRepository tenantRepository, ILogger<BulkDeactivateTenantsHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<int> Handle(BulkDeactivateTenantsCommand request, CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.GetByIdsAsync(request.TenantIds, cancellationToken);
        foreach (var tenant in tenants)
        {
            tenant.Deactivate();
        }

        await _tenantRepository.BulkUpdateAsync(tenants, cancellationToken);
        _logger.LogInformation("Bulk deactivated {Count} tenants", tenants.Count());

        return tenants.Count();
    }
}

/// <summary>
/// Handler for bulk restoring tenants
/// </summary>
public class BulkRestoreTenantsHandler : IRequestHandler<BulkRestoreTenantsCommand, int>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<BulkRestoreTenantsHandler> _logger;

    public BulkRestoreTenantsHandler(ITenantRepository tenantRepository, ILogger<BulkRestoreTenantsHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<int> Handle(BulkRestoreTenantsCommand request, CancellationToken cancellationToken)
    {
        var count = await _tenantRepository.BulkRestoreAsync(request.TenantIds, cancellationToken);
        _logger.LogInformation("Bulk restored {Count} tenants", count);

        return count;
    }
}
