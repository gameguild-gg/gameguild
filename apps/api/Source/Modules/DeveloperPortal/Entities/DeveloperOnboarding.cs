using GameGuild.Core.Domain;

namespace GameGuild.Modules.DeveloperPortal.Entities;

/// <summary>
/// Represents a developer's onboarding progress.
/// </summary>
public class DeveloperOnboarding : EntityBase
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the developer.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the current onboarding step.
    /// </summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Gets or sets the total number of onboarding steps.
    /// </summary>
    public int TotalSteps { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether onboarding is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets when onboarding was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when onboarding was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the completed steps as JSON array.
    /// </summary>
    public string? CompletedSteps { get; set; }

    /// <summary>
    /// Gets or sets whether the first API call was made.
    /// </summary>
    public bool HasMadeFirstApiCall { get; set; }

    /// <summary>
    /// Gets or sets whether the API key was created.
    /// </summary>
    public bool HasCreatedApiKey { get; set; }

    /// <summary>
    /// Gets or sets whether documentation was viewed.
    /// </summary>
    public bool HasViewedDocumentation { get; set; }

    /// <summary>
    /// Gets or sets whether a webhook was configured.
    /// </summary>
    public bool HasConfiguredWebhook { get; set; }

    /// <summary>
    /// Gets or sets whether the SDK was downloaded.
    /// </summary>
    public bool HasDownloadedSdk { get; set; }

    /// <summary>
    /// Gets or sets the preferred programming language.
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Gets or sets the use case/purpose.
    /// </summary>
    public string? UseCase { get; set; }

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Marks a step as completed and advances progress.
    /// </summary>
    public void CompleteStep(int step)
    {
        if (step > CurrentStep)
        {
            CurrentStep = step;
        }

        if (CurrentStep >= TotalSteps)
        {
            IsCompleted = true;
            CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Calculates the completion percentage.
    /// </summary>
    public double GetCompletionPercentage()
    {
        if (TotalSteps == 0) return 0;
        return (double)CurrentStep / TotalSteps * 100;
    }
}
