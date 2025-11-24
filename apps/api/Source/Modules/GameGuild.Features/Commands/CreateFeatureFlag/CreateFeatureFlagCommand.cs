using GameGuild.CQRS;

namespace GameGuild.Features.Commands;

/// <summary>
///     Command to create a new feature flag
/// </summary>
public sealed record CreateFeatureFlagCommand(string Key, string Name, string? Description, bool IsEnabled = false, Guid? TenantId = null) : IRequest<Guid>;
