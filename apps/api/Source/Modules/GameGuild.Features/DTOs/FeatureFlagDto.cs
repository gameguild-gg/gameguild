using System.Collections.ObjectModel;
using GameGuild.Features.Entities;

namespace GameGuild.Features.DTOs;

/// <summary>
///     Data Transfer Object for FeatureFlag
/// </summary>
public record FeatureFlagDto
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required bool IsEnabled { get; init; }

    public required FeatureFlagType Type { get; init; }

    public string? Environment { get; init; }

    public Guid? TenantId { get; init; }

    public object? DefaultValue { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public ReadOnlyCollection<FeatureFlagTargetDto>? Targets { get; init; }
}
