using GameGuild.CQRS;

namespace GameGuild.Features.Commands;

/// <summary>
///     Command to disable a feature flag
/// </summary>
public sealed record DisableFeatureFlagCommand(Guid Id) : IRequest;
