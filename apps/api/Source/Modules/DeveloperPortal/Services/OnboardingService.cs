using GameGuild.Modules.DeveloperPortal.Entities;
using GameGuild.Modules.DeveloperPortal.Repositories;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.DeveloperPortal.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IDeveloperOnboardingRepository _onboardingRepository;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        IDeveloperOnboardingRepository onboardingRepository,
        ILogger<OnboardingService> logger)
    {
        _onboardingRepository = onboardingRepository;
        _logger = logger;
    }

    public async Task<DeveloperOnboarding> StartOnboardingAsync(
        Guid developerId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        // Check if onboarding already exists
        var existing = await _onboardingRepository.GetByDeveloperIdAsync(developerId, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Onboarding already exists for developer {DeveloperId}", developerId);
            return existing;
        }

        var onboarding = new DeveloperOnboarding
        {
            Id = Guid.NewGuid(),
            DeveloperId = developerId,
            TenantId = tenantId,
            IsCompleted = false,
            CurrentStep = "welcome",
            CompletedSteps = new Dictionary<string, bool>
            {
                { "welcome", false },
                { "create_api_key", false },
                { "first_api_call", false },
                { "explore_docs", false },
                { "setup_webhook", false }
            },
            StartedAt = DateTime.UtcNow,
            CompletedAt = null
        };

        await _onboardingRepository.AddAsync(onboarding, cancellationToken);

        _logger.LogInformation("Started onboarding for developer {DeveloperId}", developerId);

        return onboarding;
    }

    public async Task<DeveloperOnboarding> CompleteOnboardingAsync(
        Guid developerId,
        CancellationToken cancellationToken = default)
    {
        var onboarding = await _onboardingRepository.GetByDeveloperIdAsync(developerId, cancellationToken);
        if (onboarding == null)
        {
            throw new InvalidOperationException($"No onboarding found for developer {developerId}");
        }

        if (onboarding.IsCompleted)
        {
            _logger.LogInformation("Onboarding already completed for developer {DeveloperId}", developerId);
            return onboarding;
        }

        onboarding.Complete();
        await _onboardingRepository.UpdateAsync(onboarding, cancellationToken);

        _logger.LogInformation("Completed onboarding for developer {DeveloperId}", developerId);

        return onboarding;
    }

    public async Task<DeveloperOnboarding?> GetOnboardingStatusAsync(
        Guid developerId,
        CancellationToken cancellationToken = default)
    {
        return await _onboardingRepository.GetByDeveloperIdAsync(developerId, cancellationToken);
    }

    public async Task<DeveloperOnboarding> UpdateOnboardingProgressAsync(
        Guid developerId,
        string stepKey,
        bool completed,
        CancellationToken cancellationToken = default)
    {
        var onboarding = await _onboardingRepository.GetByDeveloperIdAsync(developerId, cancellationToken);
        if (onboarding == null)
        {
            throw new InvalidOperationException($"No onboarding found for developer {developerId}");
        }

        onboarding.UpdateProgress(stepKey, completed);
        await _onboardingRepository.UpdateAsync(onboarding, cancellationToken);

        _logger.LogInformation(
            "Updated onboarding progress for developer {DeveloperId}: {StepKey} = {Completed}",
            developerId, stepKey, completed);

        return onboarding;
    }
}
