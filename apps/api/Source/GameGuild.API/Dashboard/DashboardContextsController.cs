using Asp.Versioning;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using GameGuild.TestingLab;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Dashboard;

public static class DashboardContextTypes
{
    public const string Workspace = "Workspace";
    public const string Team = "Team";
    public const string Project = "Project";
    public const string Operations = "Operations";
}

public sealed record DashboardContextSummary(
    string Type,
    Guid? Id,
    string Name,
    string Route);

public sealed record DashboardContextsResponse(
    IReadOnlyList<DashboardContextSummary> Contexts,
    IReadOnlyList<string> Capabilities,
    DashboardWorkspaceCounts Counts,
    IReadOnlyList<DashboardNavigationGroup> Navigation);

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("v{version:apiVersion}/dashboard/contexts")]
public sealed class DashboardContextsController(
    IActorContextAccessor actorContextAccessor,
    ITestingLabPermissionService testingLabPermissionService,
    IDashboardWorkspaceContextService workspaceContextService,
    ITenantMembershipChecker tenantMembershipChecker) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardContextsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardContextsResponse>> Get(
        CancellationToken cancellationToken = default)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } userId || actor.TenantId is not { } tenantId)
            return Unauthorized();
        if (!await tenantMembershipChecker.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken).ConfigureAwait(false))
            return Forbid();

        var testingPermissions = await testingLabPermissionService
            .GetUserPermissionsAsync(userId, actor.TenantId)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var capabilities = DashboardCapabilityResolver
            .Resolve(actor, testingPermissions)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var contexts = new List<DashboardContextSummary>
        {
            new(DashboardContextTypes.Workspace, null, "Workspace", "/dashboard"),
        };
        var workspace = await workspaceContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        contexts.AddRange(workspace.Contexts);

        if (capabilities.Length > 0)
        {
            contexts.Add(new DashboardContextSummary(
                DashboardContextTypes.Operations,
                null,
                "Operations",
                "/dashboard"));
        }

        return Ok(new DashboardContextsResponse(
            contexts,
            capabilities,
            workspace.Counts,
            DashboardNavigationResolver.Resolve(capabilities)));
    }
}
