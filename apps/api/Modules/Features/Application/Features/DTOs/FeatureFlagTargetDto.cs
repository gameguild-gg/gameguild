namespace GameGuild.Modules.Features.DTOs;

/// <summary>
///     Data Transfer Object for FeatureFlagTarget
/// </summary>
public sealed record FeatureFlagTargetDto
{
    public required Guid Id { get; init; }
    
    public required Guid FeatureFlagId { get; init; }
    
    public required string TargetType { get; init; }
    
    public required string TargetIdentifier { get; init; }
    
    public required bool IsEnabled { get; init; }
    
    public int RolloutPercentage { get; init; }
    
    public string? CustomValue { get; init; }
    
    public string? Metadata { get; init; }
    
    public int Priority { get; init; }
    
    public required DateTime CreatedAt { get; init; }
    
    public DateTime? UpdatedAt { get; init; }
    
    public DateTime? DeletedAt { get; init; }
}

