using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.Tenant.Models;

namespace GameGuild.Modules.Tenant.Dtos;

/// <summary>
/// DTO for creating a new tenant domain
/// </summary>
public class CreateTenantDomainDto {
  /// <summary>
  /// The top-level domain name (e.g., "champlain.edu", "university.edu")
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string TopLevelDomain { get; set; } = string.Empty;

  /// <summary>
  /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu")
  /// </summary>
  [MaxLength(100)]
  public string? Subdomain { get; set; }

  /// <summary>
  /// Whether this is the main/principal domain for the tenant (only one per tenant)
  /// </summary>
  public bool IsMainDomain { get; set; } = false;

  /// <summary>
  /// Whether this is a secondary domain for the tenant (can have multiple per tenant)
  /// </summary>
  public bool IsSecondaryDomain { get; set; } = false;

  /// <summary>
  /// ID of the tenant this domain belongs to
  /// </summary>
  [Required]
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the user group that users with this domain should be automatically added to
  /// </summary>
  public Guid? UserGroupId { get; set; }

  /// <summary>
  /// Convert DTO to domain model
  /// </summary>
  public TenantDomain ToTenantDomain() {
    return new TenantDomain {
      TopLevelDomain = TopLevelDomain,
      Subdomain = Subdomain,
      IsMainDomain = IsMainDomain,
      IsSecondaryDomain = IsSecondaryDomain,
      TenantId = TenantId,
      UserGroupId = UserGroupId
    };
  }
}

/// <summary>
/// DTO for updating an existing tenant domain
/// </summary>
public class UpdateTenantDomainDto {
  /// <summary>
  /// The top-level domain name (e.g., "champlain.edu", "university.edu")
  /// </summary>
  [MaxLength(255)]
  public string? TopLevelDomain { get; set; }

  /// <summary>
  /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu")
  /// </summary>
  [MaxLength(100)]
  public string? Subdomain { get; set; }

  /// <summary>
  /// Whether this is the main/principal domain for the tenant
  /// </summary>
  public bool? IsMainDomain { get; set; }

  /// <summary>
  /// Whether this is a secondary domain for the tenant
  /// </summary>
  public bool? IsSecondaryDomain { get; set; }

  /// <summary>
  /// ID of the user group that users with this domain should be automatically added to
  /// </summary>
  public Guid? UserGroupId { get; set; }

  /// <summary>
  /// Apply updates to domain model
  /// </summary>
  public void UpdateTenantDomain(TenantDomain domain) {
    if (!string.IsNullOrEmpty(TopLevelDomain)) domain.TopLevelDomain = TopLevelDomain;

    if (Subdomain != null) domain.Subdomain = Subdomain;

    if (IsMainDomain.HasValue) domain.IsMainDomain = IsMainDomain.Value;

    if (IsSecondaryDomain.HasValue) domain.IsSecondaryDomain = IsSecondaryDomain.Value;

    if (UserGroupId.HasValue) domain.UserGroupId = UserGroupId.Value;
  }
}

/// <summary>
/// DTO for creating a new tenant user group
/// </summary>
public class CreateTenantUserGroupDto {
  /// <summary>
  /// Name of the user group (e.g., "Students", "Professors", "Administrators")
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of what this group represents
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// ID of the tenant this group belongs to
  /// </summary>
  [Required]
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the parent group (for nested group hierarchy)
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// Whether this is the default group for auto-assignment
  /// </summary>
  public bool IsDefault { get; set; } = false;

  /// <summary>
  /// Whether this group is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Convert DTO to domain model
  /// </summary>
  public TenantUserGroup ToTenantUserGroup() {
    return new TenantUserGroup {
      Name = Name,
      Description = Description,
      TenantId = TenantId,
      ParentGroupId = ParentGroupId,
      IsDefault = IsDefault,
      IsActive = IsActive
    };
  }
}

/// <summary>
/// DTO for updating an existing tenant user group
/// </summary>
public class UpdateTenantUserGroupDto {
  /// <summary>
  /// Name of the user group
  /// </summary>
  [MaxLength(100)]
  public string? Name { get; set; }

  /// <summary>
  /// Description of what this group represents
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// ID of the parent group (for nested group hierarchy)
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// Whether this group is currently active
  /// </summary>
  public bool? IsActive { get; set; }

  /// <summary>
  /// Apply updates to user group model
  /// </summary>
  public void UpdateTenantUserGroup(TenantUserGroup userGroup) {
    if (!string.IsNullOrEmpty(Name)) userGroup.Name = Name;

    if (Description != null) userGroup.Description = Description;

    if (ParentGroupId.HasValue) userGroup.ParentGroupId = ParentGroupId.Value;

    if (IsActive.HasValue) userGroup.IsActive = IsActive.Value;
  }
}

/// <summary>
/// DTO for adding a user to a group
/// </summary>
public class AddUserToGroupDto {
  /// <summary>
  /// ID of the user to add
  /// </summary>
  [Required]
  public Guid UserId { get; set; }

  /// <summary>
  /// ID of the group to add the user to
  /// </summary>
  [Required]
  public Guid UserGroupId { get; set; }

  /// <summary>
  /// Whether this assignment should be marked as auto-assigned
  /// </summary>
  public bool IsAutoAssigned { get; set; } = false;
}

/// <summary>
/// DTO for auto-assignment request
/// </summary>
public class AutoAssignUsersDto {
  /// <summary>
  /// Optional tenant ID to limit auto-assignment to specific tenant
  /// </summary>
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Optional list of user emails to specifically auto-assign
  /// </summary>
  public List<string>? UserEmails { get; set; }
}

/// <summary>
/// DTO for tenant domain response
/// </summary>
public class TenantDomainDto {
  /// <summary>
  /// Domain ID
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// The top-level domain name (e.g., "champlain.edu", "university.edu")
  /// </summary>
  public string TopLevelDomain { get; set; } = string.Empty;

  /// <summary>
  /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu")
  /// </summary>
  public string? Subdomain { get; set; }

  /// <summary>
  /// Whether this is the main/principal domain for the tenant
  /// </summary>
  public bool IsMainDomain { get; set; }

  /// <summary>
  /// Whether this is a secondary domain for the tenant
  /// </summary>
  public bool IsSecondaryDomain { get; set; }

  /// <summary>
  /// ID of the tenant this domain belongs to
  /// </summary>
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the user group that users with this domain should be automatically added to
  /// </summary>
  public Guid? UserGroupId { get; set; }

  /// <summary>
  /// When the domain was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the domain was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Create from domain model
  /// </summary>
  public static TenantDomainDto FromTenantDomain(TenantDomain domain) {
    return new TenantDomainDto {
      Id = domain.Id,
      TopLevelDomain = domain.TopLevelDomain,
      Subdomain = domain.Subdomain,
      IsMainDomain = domain.IsMainDomain,
      IsSecondaryDomain = domain.IsSecondaryDomain,
      TenantId = domain.TenantId,
      UserGroupId = domain.UserGroupId,
      CreatedAt = domain.CreatedAt,
      UpdatedAt = domain.UpdatedAt
    };
  }
}

/// <summary>
/// DTO for tenant user group response
/// </summary>
public class TenantUserGroupDto {
  /// <summary>
  /// Group ID
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Group name
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Group description
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Whether this is the default group for auto-assignment
  /// </summary>
  public bool IsDefault { get; set; } = false;

  /// <summary>
  /// Parent group ID if this is a subgroup
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// ID of the tenant this group belongs to
  /// </summary>
  public Guid TenantId { get; set; }

  /// <summary>
  /// Whether the group is active
  /// </summary>
  public bool IsActive { get; set; }

  /// <summary>
  /// When the group was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the group was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Create from group model
  /// </summary>
  public static TenantUserGroupDto FromTenantUserGroup(TenantUserGroup group) {
    return new TenantUserGroupDto {
      Id = group.Id,
      Name = group.Name,
      Description = group.Description,
      IsDefault = group.IsDefault,
      ParentGroupId = group.ParentGroupId,
      TenantId = group.TenantId,
      IsActive = group.IsActive,
      CreatedAt = group.CreatedAt,
      UpdatedAt = group.UpdatedAt
    };
  }
}

/// <summary>
/// DTO for tenant user group membership response
/// </summary>
public class TenantUserGroupMembershipDto {
  /// <summary>
  /// Membership ID
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// User ID
  /// </summary>
  public Guid UserId { get; set; }

  /// <summary>
  /// Group ID
  /// </summary>
  public Guid GroupId { get; set; }

  /// <summary>
  /// Whether this membership was auto-assigned
  /// </summary>
  public bool IsAutoAssigned { get; set; }

  /// <summary>
  /// When the membership was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the membership was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Create from membership model
  /// </summary>
  public static TenantUserGroupMembershipDto FromTenantUserGroupMembership(TenantUserGroupMembership membership) {
    return new TenantUserGroupMembershipDto {
      Id = membership.Id,
      UserId = membership.UserId,
      GroupId = membership.UserGroupId,
      IsAutoAssigned = membership.IsAutoAssigned,
      CreatedAt = membership.CreatedAt,
      UpdatedAt = membership.UpdatedAt
    };
  }
}

/// <summary>
/// DTO for auto-assignment of a user
/// </summary>
public class AutoAssignUserDto {
  /// <summary>
  /// ID of the user to auto-assign
  /// </summary>
  [Required]
  public Guid UserId { get; set; }

  /// <summary>
  /// Email of the user to determine domain-based group assignment
  /// </summary>
  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;
}
