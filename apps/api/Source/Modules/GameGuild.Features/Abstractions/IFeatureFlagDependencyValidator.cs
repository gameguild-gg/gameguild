namespace GameGuild.Features;

/// <summary>
///     Service for detecting circular dependencies in feature flags
/// </summary>
public interface IFeatureFlagDependencyValidator
{
    /// <summary>
    ///     Detects if adding a dependency would create a circular dependency
    /// </summary>
    Task<bool> HasCircularDependencyAsync(string flagKey, string dependsOnKey);

    /// <summary>
    ///     Gets all circular dependencies in the system
    /// </summary>
    Task<List<List<string>>> GetAllCircularDependenciesAsync();

    /// <summary>
    ///     Validates that a dependency graph has no cycles
    /// </summary>
    Task<(bool IsValid, List<string>? Cycle)> ValidateDependencyGraphAsync(string startFlagKey);
}
