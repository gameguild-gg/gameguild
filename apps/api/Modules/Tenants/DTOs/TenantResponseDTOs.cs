using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Tenants;

/// <summary> Enhanced DTO for tenant information with computed properties </summary>
public class TenantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? AdminEmail { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    
    // Computed properties
    public int MemberCount { get; init; }
    public int ActiveMemberCount { get; init; }
    public int DomainCount { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public string Status { get; init; } = string.Empty; // Active, Inactive, Archived
    public long StorageUsed { get; init; }
    public TenantSettingsDto? Settings { get; init; }
}

/// <summary> Detailed tenant DTO with full information </summary>
public class TenantDetailDto : TenantDto
{
    public IEnumerable<TenantMemberDto> Members { get; init; } = Enumerable.Empty<TenantMemberDto>();
    public IEnumerable<TenantDomainDto> Domains { get; init; } = Enumerable.Empty<TenantDomainDto>();
    public TenantStatisticsDto? Statistics { get; init; }
}

/// <summary> DTO for tenant member information </summary>
public class TenantMemberDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime JoinedAt { get; init; }
    public DateTime? LeftAt { get; init; }
    public string? LeaveReason { get; init; }
    public Guid? ParentMemberId { get; init; }
    
    // User information (if available)
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserDisplayName { get; init; }
    
    // Hierarchy information
    public string? ParentMemberName { get; init; }
    public int ChildMemberCount { get; init; }
}

/// <summary> DTO for tenant domain information </summary>
public class TenantDomainDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TopLevelDomain { get; init; } = string.Empty;
    public string? Subdomain { get; init; }
    public string FullDomain { get; init; } = string.Empty;
    public bool IsMainDomain { get; init; }
    public bool IsSecondaryDomain { get; init; }
    public Guid? UserGroupId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary> DTO for tenant settings information </summary>
public class TenantSettingsDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string DefaultLanguage { get; init; } = string.Empty;
    public string DefaultTimezone { get; init; } = string.Empty;
    public string DefaultCurrency { get; init; } = string.Empty;
    public bool AllowUserRegistration { get; init; }
    public bool RequireRegistrationApproval { get; init; }
    public bool RequireTwoFactorAuth { get; init; }
    public int? MaxUsers { get; init; }
    public long? StorageQuota { get; init; }
    public bool EnableAuditLogging { get; init; }
    public bool EnableApiAccess { get; init; }
    public string? BrandingSettings { get; init; }
    public string? NotificationSettings { get; init; }
    public string? SecuritySettings { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary> DTO for tenant statistics </summary>
public class TenantStatisticsDto
{
    public Guid TenantId { get; init; }
    public int TotalMembers { get; init; }
    public int ActiveMembers { get; init; }
    public int InactiveMembers { get; init; }
    public int TotalDomains { get; init; }
    public long StorageUsed { get; init; }
    public long StorageQuota { get; init; }
    public int ApiCallsThisMonth { get; init; }
    public int ApiCallsTotal { get; init; }
    public DateTime LastActivityAt { get; init; }
    public TimeSpan TenantAge { get; init; }
    public Dictionary<string, int> MembersByRole { get; init; } = new();
    public Dictionary<string, object> CustomMetrics { get; init; } = new();
}

/// <summary> Request DTO for creating a tenant </summary>
public class CreateTenantRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [StringLength(255)]
    public string? Slug { get; init; }

    [EmailAddress]
    [StringLength(255)]
    public string? AdminEmail { get; init; }

    public bool IsActive { get; init; } = true;

    public TenantSettingsDto? Settings { get; init; }
    public IEnumerable<string>? InitialDomains { get; init; }
}

/// <summary> Request DTO for updating a tenant </summary>
public class UpdateTenantRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [EmailAddress]
    [StringLength(255)]
    public string? AdminEmail { get; init; }
}

/// <summary> Response DTO for bulk operations </summary>
public class BulkOperationResponseDto
{
    public int TotalRequested { get; init; }
    public int SuccessfulOperations { get; init; }
    public int FailedOperations { get; init; }
    public double SuccessRate { get; init; }
    public bool IsComplete { get; init; }
    public IEnumerable<BulkOperationErrorDto> Errors { get; init; } = Enumerable.Empty<BulkOperationErrorDto>();
    public DateTime ProcessedAt { get; init; }
    public TimeSpan ProcessingTime { get; init; }
}

/// <summary> Error DTO for bulk operations </summary>
public class BulkOperationErrorDto
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public DateTime ErrorOccurredAt { get; init; }
}