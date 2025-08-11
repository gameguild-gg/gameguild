namespace GameGuild.Modules.Tenants.Extensions;

/// <summary>
/// Extension methods for mapping between DTOs and entities in the Tenant module
/// </summary>
public static class TenantDomainMappingExtensions {
  /// <summary>
  /// Maps CreateTenantDomainDto to TenantDomain entity
  /// </summary>
  public static TenantDomain ToTenantDomain(this CreateTenantDomainDto dto) {
    return new TenantDomain {
      TenantId = dto.TenantId,
      TopLevelDomain = dto.TopLevelDomain,
      Subdomain = dto.Subdomain,
      IsMainDomain = dto.IsMainDomain,
      IsSecondaryDomain = dto.IsSecondaryDomain,
      UserGroupId = dto.UserGroupId,
    };
  }

  /// <summary>
  /// Updates TenantDomain entity with values from UpdateTenantDomainDto
  /// </summary>
  public static TenantDomain UpdateTenantDomain(this UpdateTenantDomainDto dto, TenantDomain domain) {
    if (dto.TopLevelDomain != null) domain.TopLevelDomain = dto.TopLevelDomain;

    if (dto.Subdomain != null) domain.Subdomain = dto.Subdomain;

    if (dto.IsMainDomain.HasValue) domain.IsMainDomain = dto.IsMainDomain.Value;

    if (dto.IsSecondaryDomain.HasValue) domain.IsSecondaryDomain = dto.IsSecondaryDomain.Value;

    if (dto.UserGroupId.HasValue) domain.UserGroupId = dto.UserGroupId.Value;

    return domain;
  }

  /// <summary>
  /// Maps CreateTenantUserGroupDto to TenantUserGroup entity
  /// </summary>
  public static TenantUserGroup ToTenantUserGroup(this CreateTenantUserGroupDto dto) {
    return new TenantUserGroup {
      Name = dto.Name,
      Description = dto.Description,
      TenantId = dto.TenantId,
      ParentGroupId = dto.ParentGroupId,
      IsActive = dto.IsActive,
      IsDefault = dto.IsDefault,
    };
  }

  /// <summary>
  /// Updates TenantUserGroup entity with values from UpdateTenantUserGroupDto
  /// </summary>
  public static TenantUserGroup UpdateTenantUserGroup(this UpdateTenantUserGroupDto dto, TenantUserGroup userGroup) {
    if (dto.Name != null) userGroup.Name = dto.Name;

    if (dto.Description != null) userGroup.Description = dto.Description;

    if (dto.ParentGroupId.HasValue) userGroup.ParentGroupId = dto.ParentGroupId;

    if (dto.IsActive.HasValue) userGroup.IsActive = dto.IsActive.Value;

    if (dto.IsDefault.HasValue) userGroup.IsDefault = dto.IsDefault.Value;

    return userGroup;
  }
}
