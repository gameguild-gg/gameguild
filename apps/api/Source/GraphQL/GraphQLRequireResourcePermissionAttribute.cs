using System;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;

namespace GameGuild.Authorization;

/// <summary>
/// Wrapper attribute for GraphQL resolvers to avoid name resolution conflicts with MVC attributes.
/// Delegates to existing generic RequireResourcePermissionAttribute while providing a distinct type name.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class GraphQLRequireResourcePermissionAttribute<TPermission, TEntity> : RequireResourcePermissionAttribute<TPermission, TEntity>
  where TPermission : ResourcePermission<TEntity>
  where TEntity : EntityBase {
    public GraphQLRequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id")
        : base(requiredPermission, resourceIdParameterName) { }
}
