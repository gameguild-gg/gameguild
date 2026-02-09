using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to validate feature flag key
/// </summary>
public sealed record ValidateFeatureFlagKeyQuery : IQuery<ValidationResult>
{
    public required string Key { get; init; }

    public Guid? ExcludeId { get; init; }
}
