namespace GameGuild.Features;

/// <summary>
///     Request model for creating a new feature flag
/// </summary>
public sealed record CreateFeatureRequest(string Key, string Name, string? Description, bool IsEnabled = false, Guid? TenantId = null);
