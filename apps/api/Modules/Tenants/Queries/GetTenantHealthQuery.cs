using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Query to get tenant health check information </summary>
public class GetTenantHealthQuery : IQuery<Result<TenantHealthDto>>
{
    public Guid TenantId { get; init; }
}

/// <summary> Tenant health information </summary>
public class TenantHealthDto
{
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsHealthy { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime LastChecked { get; init; }
    
    // Health indicators
    public IEnumerable<HealthIndicatorDto> HealthIndicators { get; init; } = Enumerable.Empty<HealthIndicatorDto>();
    
    // Issues found
    public IEnumerable<HealthIssueDto> Issues { get; init; } = Enumerable.Empty<HealthIssueDto>();
}

/// <summary> Health indicator information </summary>
public class HealthIndicatorDto
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // Healthy, Warning, Critical
    public string? Message { get; init; }
    public DateTime LastChecked { get; init; }
}

/// <summary> Health issue information </summary>
public class HealthIssueDto
{
    public string Severity { get; init; } = string.Empty; // Low, Medium, High, Critical
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Recommendation { get; init; }
    public DateTime DetectedAt { get; init; }
}