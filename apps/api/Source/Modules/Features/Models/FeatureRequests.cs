namespace GameGuild.Modules.Features.Models;

/// <summary>
/// Request model for creating a new feature flag
/// </summary>
public record CreateFeatureRequest(
    string Key, 
    string Name, 
    string? Description, 
    bool IsEnabled = false, 
    Guid? TenantId = null
);

/// <summary>
/// Request model for toggling a feature flag
/// </summary>
public record ToggleFeatureRequest(bool IsEnabled);

