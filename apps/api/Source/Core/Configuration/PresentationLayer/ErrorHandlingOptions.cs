namespace GameGuild;

/// <summary>
/// Configuration options for global error handling features
/// </summary>
public sealed class ErrorHandlingOptions {
    /// <summary>
    /// Enable enhanced exception handling with detailed error information
    /// </summary>
    public bool EnableEnhancedExceptionHandling { get; set; } = true;

    /// <summary>
    /// Enable RFC 7807 ProblemDetails compliance for error responses
    /// </summary>
    public bool EnableProblemDetailsCompliance { get; set; } = true;

    /// <summary>
    /// Enable Result pattern mapping for consistent API responses
    /// </summary>
    public bool EnableResultPatternMapping { get; set; } = true;

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    public void Validate() {
        // Validation logic can be added here if needed
        // Currently no validation constraints for boolean flags
    }
}
