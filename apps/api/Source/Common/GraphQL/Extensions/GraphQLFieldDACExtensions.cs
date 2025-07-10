using GameGuild.Common.Authorization;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Resources;
using HotChocolate.Language;


namespace GameGuild.Common.Extensions;

/// <summary>
/// Extension methods for applying DAC authorization to GraphQL field descriptors
/// </summary>
public static class GraphQLFieldDACExtensions {
  /// <summary>
  /// Applies tenant-level DAC authorization to a GraphQL field
  /// </summary>
  /// <param name="descriptor">The field descriptor</param>
  /// <param name="permission">The required permission</param>
  /// <returns>The field descriptor for method chaining</returns>
  public static IObjectFieldDescriptor RequireTenantPermission(
    this IObjectFieldDescriptor descriptor,
    PermissionType permission
  ) {
    return descriptor.Directive(
      "dacAuthorize",
      new ArgumentNode("level", new StringValueNode(DACPermissionLevel.Tenant.ToString())),
      new ArgumentNode("permission", new StringValueNode(permission.ToString()))
    );
  }

  /// <summary>
  /// Applies content-type level DAC authorization to a GraphQL field
  /// </summary>
  /// <typeparam name="TEntity">The entity type</typeparam>
  /// <param name="descriptor">The field descriptor</param>
  /// <param name="permission">The required permission</param>
  /// <returns>The field descriptor for method chaining</returns>
  public static IObjectFieldDescriptor RequireContentTypePermission<TEntity>(
    this IObjectFieldDescriptor descriptor,
    PermissionType permission
  ) where TEntity : class {
    return descriptor.Directive(
      "dacAuthorize",
      new ArgumentNode("level", new StringValueNode(DACPermissionLevel.ContentType.ToString())),
      new ArgumentNode("permission", new StringValueNode(permission.ToString())),
      new ArgumentNode("entityType", new StringValueNode(typeof(TEntity).Name))
    );
  }

  /// <summary>
  /// Applies resource-level DAC authorization to a GraphQL field
  /// </summary>
  /// <typeparam name="TPermission">The permission entity type</typeparam>
  /// <typeparam name="TEntity">The resource entity type</typeparam>
  /// <param name="descriptor">The field descriptor</param>
  /// <param name="permission">The required permission</param>
  /// <param name="resourceIdParameter">The parameter name containing the resource ID</param>
  /// <returns>The field descriptor for method chaining</returns>
  public static IObjectFieldDescriptor RequireResourcePermission<TPermission, TEntity>(
    this IObjectFieldDescriptor descriptor, PermissionType permission,
    string resourceIdParameter = "id"
  )
    where TPermission : ResourcePermission<TEntity>
    where TEntity : Entity {
    return descriptor.Directive(
      "dacAuthorize",
      new ArgumentNode("level", new StringValueNode(DACPermissionLevel.Resource.ToString())),
      new ArgumentNode("permission", new StringValueNode(permission.ToString())),
      new ArgumentNode("entityType", new StringValueNode(typeof(TEntity).Name)),
      new ArgumentNode("permissionType", new StringValueNode(typeof(TPermission).Name)),
      new ArgumentNode("resourceIdParameter", new StringValueNode(resourceIdParameter))
    );
  }

  /// <summary>
  /// Applies resource-level DAC authorization to a GraphQL field (inferred permission type)
  /// </summary>
  /// <typeparam name="TEntity">The resource entity type</typeparam>
  /// <param name="descriptor">The field descriptor</param>
  /// <param name="permission">The required permission</param>
  /// <param name="resourceIdParameter">The parameter name containing the resource ID</param>
  /// <returns>The field descriptor for method chaining</returns>
  public static IObjectFieldDescriptor RequireResourcePermission<TEntity>(
    this IObjectFieldDescriptor descriptor,
    PermissionType permission, string resourceIdParameter = "id"
  )
    where TEntity : Entity {
    return descriptor.Directive(
      "dacAuthorize",
      new ArgumentNode("level", new StringValueNode(DACPermissionLevel.Resource.ToString())),
      new ArgumentNode("permission", new StringValueNode(permission.ToString())),
      new ArgumentNode("entityType", new StringValueNode(typeof(TEntity).Name)),
      new ArgumentNode("resourceIdParameter", new StringValueNode(resourceIdParameter))
    );
  }
}
