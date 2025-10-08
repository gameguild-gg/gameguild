using MediatR;
using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get feature flag configurations for SDK
/// </summary>
public sealed record GetFeatureFlagConfigsQuery : IRequest<IEnumerable<FeatureFlagConfig>>
{
    public required string Environment { get; init; }

    public string? TenantId { get; init; }

    public IEnumerable<string>? FeatureKeys { get; init; }

    public DateTime? ModifiedSince { get; init; }
}

