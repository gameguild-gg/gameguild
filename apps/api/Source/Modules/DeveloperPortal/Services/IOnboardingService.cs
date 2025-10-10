using GameGuild.Modules.DeveloperPortal.Entities;

namespace GameGuild.Modules.DeveloperPortal.Services;

public interface IOnboardingService
{
    Task<DeveloperOnboarding> StartOnboardingAsync(Guid developerId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<DeveloperOnboarding> CompleteOnboardingAsync(Guid developerId, CancellationToken cancellationToken = default);
    Task<DeveloperOnboarding?> GetOnboardingStatusAsync(Guid developerId, CancellationToken cancellationToken = default);
    Task<DeveloperOnboarding> UpdateOnboardingProgressAsync(Guid developerId, string stepKey, bool completed, CancellationToken cancellationToken = default);
}
