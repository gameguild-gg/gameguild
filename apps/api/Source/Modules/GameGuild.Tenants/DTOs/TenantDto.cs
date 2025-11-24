namespace GameGuild.Tenants.DTOs;

/// <summary>
///     DTO for tenant information
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public TenantSubscriptionDto? CurrentPlan { get; set; }

    public int UsersCount { get; set; }
}
