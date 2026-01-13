namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of validating plan limits
/// </summary>
public class PlanValidationResult
{
    /// <summary>
    ///     Whether the plan can support the requested limits
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    ///     List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    ///     Suggested plan upgrades if current plan is insufficient
    /// </summary>
    public List<Guid> SuggestedUpgrades { get; set; } = new List<Guid>();

    /// <summary>
    ///     Creates a successful validation result
    /// </summary>
    public static PlanValidationResult Success() { return new PlanValidationResult { IsValid = true }; }

    /// <summary>
    ///     Creates a failed validation result with errors
    /// </summary>
    public static PlanValidationResult Failure(params string[ ] errors) { return new PlanValidationResult { IsValid = false, Errors = errors.ToList() }; }

    /// <summary>
    ///     Creates a failed validation result with suggested upgrades
    /// </summary>
    public static PlanValidationResult FailureWithSuggestions(List<string> errors, List<Guid> suggestedUpgrades)
    {
        return new PlanValidationResult { IsValid = false, Errors = errors, SuggestedUpgrades = suggestedUpgrades };
    }
}
