using GameGuild.Database;
using Microsoft.EntityFrameworkCore;
using GameGuild.Modules.DeveloperPortal.Entities;

namespace GameGuild.Modules.DeveloperPortal.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ApplicationDbContext _context;

    public ApiKeyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApiKey>()
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
    }

    public async Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApiKey>()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);
    }

    public async Task<List<ApiKey>> GetByDeveloperIdAsync(
        Guid developerId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ApiKey>()
            .Where(k => k.DeveloperId == developerId);

        if (!includeRevoked)
        {
            query = query.Where(k => !k.IsRevoked);
        }

        return await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        await _context.Set<ApiKey>().AddAsync(apiKey, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _context.Set<ApiKey>().Update(apiKey);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ApiUsageLogRepository : IApiUsageLogRepository
{
    private readonly ApplicationDbContext _context;

    public ApiUsageLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ApiUsageLog log, CancellationToken cancellationToken = default)
    {
        await _context.Set<ApiUsageLog>().AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ApiUsageLog>> GetByApiKeyIdsAsync(
        List<Guid> apiKeyIds,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ApiUsageLog>()
            .Where(l => apiKeyIds.Contains(l.ApiKeyId));

        if (startDate.HasValue)
        {
            query = query.Where(l => l.RequestedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.RequestedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(l => l.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApiUsageLog>> GetByApiKeyIdsAsync(
        List<Guid> apiKeyIds,
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ApiUsageLog>()
            .Where(l => apiKeyIds.Contains(l.ApiKeyId));

        if (startDate.HasValue)
        {
            query = query.Where(l => l.RequestedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.RequestedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(l => l.RequestedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

public class DeveloperOnboardingRepository : IDeveloperOnboardingRepository
{
    private readonly ApplicationDbContext _context;

    public DeveloperOnboardingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeveloperOnboarding?> GetByDeveloperIdAsync(
        Guid developerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeveloperOnboarding>()
            .FirstOrDefaultAsync(o => o.DeveloperId == developerId, cancellationToken);
    }

    public async Task AddAsync(DeveloperOnboarding onboarding, CancellationToken cancellationToken = default)
    {
        await _context.Set<DeveloperOnboarding>().AddAsync(onboarding, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DeveloperOnboarding onboarding, CancellationToken cancellationToken = default)
    {
        _context.Set<DeveloperOnboarding>().Update(onboarding);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
