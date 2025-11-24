using GameGuild.CQRS;

namespace GameGuild.Features.Commands;

/// <summary>
///     Command to enable a feature flag
/// </summary>
public sealed record EnableFeatureFlagCommand(Guid Id) : IRequest;
