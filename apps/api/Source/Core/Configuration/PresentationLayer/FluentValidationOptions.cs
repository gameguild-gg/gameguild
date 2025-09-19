namespace GameGuild;

/// <summary>
/// Configuration options for FluentValidation features
/// </summary>
public sealed class FluentValidationOptions {
    /// <summary>
    /// Enable validation pipeline behavior for CQRS commands and queries
    /// </summary>
    public bool EnableValidationBehavior { get; set; } = true;

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    public void Validate() {
        // Validation logic can be added here if needed
        // Currently no validation constraints for boolean flags
    }
}
