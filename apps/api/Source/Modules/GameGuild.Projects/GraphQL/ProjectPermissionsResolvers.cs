using System.Security.Claims;
using GameGuild.Identity.Authorization;
using HotChocolate;
using HotChocolate.Types;

namespace GameGuild.Projects;

[ExtendObjectType(typeof(Project))]
public sealed class ProjectPermissionsResolvers {
  public async Task<bool> CanEdit([Service] IProjectAuthorizationService authorizationService, ClaimsPrincipal user, [Parent] Project project) {
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(project);

    if (user?.Identity?.IsAuthenticated != true) { return false; }

    return await authorizationService.HasPermissionAsync(project.Id, PermissionType.Edit).ConfigureAwait(false);
  }

  public async Task<bool> CanDelete([Service] IProjectAuthorizationService authorizationService, ClaimsPrincipal user, [Parent] Project project) {
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(project);

    if (user?.Identity?.IsAuthenticated != true) { return false; }

    return await authorizationService.HasPermissionAsync(project.Id, PermissionType.Delete).ConfigureAwait(false);
  }
}
