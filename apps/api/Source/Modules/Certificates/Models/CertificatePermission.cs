namespace GameGuild.Modules.Certificates;

/// <summary> Permission class for Certificate entities Manages access control for certificates </summary>
public class CertificatePermission : ResourcePermission<Certificate> {
  /// <summary> Initialize Certificate permission </summary>
  /// <param name="permissionType"> The type of permission required </param>
  public CertificatePermission(PermissionType permissionType) : base(permissionType) { }
}
