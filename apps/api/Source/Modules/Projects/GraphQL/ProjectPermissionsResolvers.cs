using System.Security.Claims;
using HotChocolate;
using HotChocolate.Types;
using GameGuild.Modules.Permissions;
using GameGuild.Common;
using GameGuild.Modules.Authentication;

namespace GameGuild.Modules.Projects {
  [ExtendObjectType(typeof(Project))]
  public sealed class ProjectPermissionsResolvers {
    public async Task<bool> CanEdit(
      [Service] IPermissionService permissionService,
      ClaimsPrincipal user,
      [Parent] Project project
    ) {
      ArgumentNullException.ThrowIfNull(permissionService);
      ArgumentNullException.ThrowIfNull(project);

      if (user?.Identity?.IsAuthenticated != true) {
        return false;
      }

      var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var tenantIdClaim = user.FindFirst(JwtClaimTypes.TenantId)?.Value;
      var tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : (Guid?)null;

      return await permissionService.HasResourcePermissionAsync<ProjectPermission, Project>(userId, tenantId, project.Id, PermissionType.Edit)
                                    .ConfigureAwait(false);
    }

    public async Task<bool> CanDelete(
      [Service] IPermissionService permissionService,
      ClaimsPrincipal user,
      [Parent] Project project
    ) {
      ArgumentNullException.ThrowIfNull(permissionService);
      ArgumentNullException.ThrowIfNull(project);

      if (user?.Identity?.IsAuthenticated != true) {
        return false;
      }

      var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var tenantIdClaim = user.FindFirst(JwtClaimTypes.TenantId)?.Value;
      var tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : (Guid?)null;

      return await permissionService.HasResourcePermissionAsync<ProjectPermission, Project>(userId, tenantId, project.Id, PermissionType.Delete)
                                    .ConfigureAwait(false);
    }
  }
}
