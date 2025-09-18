using GameGuild.Modules.Permissions;

namespace GameGuild.Modules.Certificates;

/// <summary> Permission class for Certificate entities Manages access control for certificates </summary>
public class CertificatePermission : ResourcePermission<Certificate> {
  // Public parameterless constructor for EF and GraphQL
  public CertificatePermission() { }

  // Public constructor for creating instances
  public CertificatePermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions)
    : base(userId, tenantId, resourceId, permissions) { }
}
