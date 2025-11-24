namespace GameGuild.Features.Models;

/// <summary>
///     Request model for creating a new feature flag
/// </summary>
public record CreateFeatureRequest(string Key, string Name, string? Description, bool IsEnabled = false, Guid? TenantId = null);
