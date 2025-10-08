using MediatR;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to check if feature flag exists
/// </summary>
public sealed record FeatureFlagExistsQuery : IRequest<bool>
{
    public required string Key { get; init; }

    public string? Environment { get; init; }
}

