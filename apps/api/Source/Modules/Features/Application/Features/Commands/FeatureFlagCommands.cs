using GameGuild.CQRS;

namespace GameGuild.Modules.Features.Commands;

/// <summary>
///     Command to create a new feature flag
/// </summary>
public sealed record CreateFeatureFlagCommand(
    string Key,
    string Name,
    string? Description,
    bool IsEnabled = false,
    Guid? TenantId = null
) : IRequest<Guid>;

/// <summary>
///     Command to enable a feature flag
/// </summary>
public sealed record EnableFeatureFlagCommand(Guid Id) : IRequest;

/// <summary>
///     Command to disable a feature flag
/// </summary>
public sealed record DisableFeatureFlagCommand(Guid Id) : IRequest;

/// <summary>
///     Command to toggle a feature flag state
/// </summary>
public sealed record ToggleFeatureFlagCommand(Guid Id, bool IsEnabled) : IRequest;

