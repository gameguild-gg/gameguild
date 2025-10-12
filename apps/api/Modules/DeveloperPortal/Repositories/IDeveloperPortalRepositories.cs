using GameGuild.Modules.DeveloperPortal.Entities;

namespace GameGuild.Modules.DeveloperPortal.Repositories;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<List<ApiKey>> GetByDeveloperIdAsync(Guid developerId, bool includeRevoked = false, CancellationToken cancellationToken = default);
    Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
}

public interface IApiUsageLogRepository
{
    Task AddAsync(ApiUsageLog log, CancellationToken cancellationToken = default);
    Task<List<ApiUsageLog>> GetByApiKeyIdsAsync(List<Guid> apiKeyIds, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<ApiUsageLog>> GetByApiKeyIdsAsync(List<Guid> apiKeyIds, DateTime? startDate, DateTime? endDate, int skip, int take, CancellationToken cancellationToken = default);
}

public interface IDeveloperOnboardingRepository
{
    Task<DeveloperOnboarding?> GetByDeveloperIdAsync(Guid developerId, CancellationToken cancellationToken = default);
    Task AddAsync(DeveloperOnboarding onboarding, CancellationToken cancellationToken = default);
    Task UpdateAsync(DeveloperOnboarding onboarding, CancellationToken cancellationToken = default);
}
