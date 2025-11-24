using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to search feature flags
/// </summary>
public record SearchFeatureFlagsQuery : IQuery<PagedResult<FeatureFlagDto>>
{
    public string? SearchTerm { get; init; }

    public string? Environment { get; init; }

    public bool? IsEnabled { get; init; }

    public bool? IsGlobal { get; init; }

    public string? Type { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
