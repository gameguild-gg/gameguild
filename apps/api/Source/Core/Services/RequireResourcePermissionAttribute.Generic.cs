

// Compatibility shim: some code (or generated code) expects generic RequireResourcePermissionAttribute
// in the root GameGuild namespace. The canonical generic implementation now lives in
// GameGuild.Authorization. This bridge preserves old references without reintroducing
// the non-generic name collision we renamed (RequireDacResourcePermissionAttribute).
namespace GameGuild;

public class RequireResourcePermissionAttribute<TPermission, TEntity> : Authorization.RequireResourcePermissionAttribute<TPermission, TEntity> where TPermission : ResourcePermission<TEntity> where TEntity : EntityBase {
  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") : base(requiredPermission, resourceIdParameterName) { }
}
