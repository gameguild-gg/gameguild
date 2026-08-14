using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using GameGuild.Projects;

namespace GameGuild.LaunchPad;

public static class LaunchPadCapabilities
{
    public const string Participate = "LaunchPad.Participate";
    public const string ManageEvents = "LaunchPad.ManageEvents";
    public const string ReviewApplications = "LaunchPad.ReviewApplications";
    public const string ManageParticipants = "LaunchPad.ManageParticipants";
    public const string ViewAnalytics = "LaunchPad.ViewAnalytics";
    public const string ManageSettings = "LaunchPad.ManageSettings";
}

public interface ILaunchPadAuthorizationService
{
    Task<bool> CanManageEventsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanReviewApplicationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanManageParticipantsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanViewAnalyticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanParticipateAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanSubmitProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public sealed class LaunchPadAuthorizationService(
    IActorContextAccessor actors,
    IProjectAuthorizationService projects) : ILaunchPadAuthorizationService
{
    public Task<bool> CanManageEventsAsync(Guid tenantId, CancellationToken cancellationToken = default) => HasAdministrativeCapabilityAsync(
        tenantId, cancellationToken, LaunchPadCapabilities.ManageEvents, "launchpad:events:manage", "launchpad:events:create", "launchpad:events:update");

    public Task<bool> CanReviewApplicationsAsync(Guid tenantId, CancellationToken cancellationToken = default) => HasAdministrativeCapabilityAsync(
        tenantId, cancellationToken, LaunchPadCapabilities.ReviewApplications, "launchpad:applications:review", "launchpad:applications:manage");

    public Task<bool> CanManageParticipantsAsync(Guid tenantId, CancellationToken cancellationToken = default) => HasAdministrativeCapabilityAsync(
        tenantId, cancellationToken, LaunchPadCapabilities.ManageParticipants, "launchpad:participants:manage");

    public Task<bool> CanViewAnalyticsAsync(Guid tenantId, CancellationToken cancellationToken = default) => HasAdministrativeCapabilityAsync(
        tenantId, cancellationToken, LaunchPadCapabilities.ViewAnalytics, "launchpad:analytics:read");

    public async Task<bool> CanParticipateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var actor = actors.ActorContext;
        return actor.IsAuthenticated && actor.SubjectIdAsGuid.HasValue &&
               actor.TenantId == tenantId &&
               await projects.IsActorActiveTenantMemberAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> CanSubmitProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        => await projects.HasPermissionAsync(projectId, PermissionType.Edit, cancellationToken).ConfigureAwait(false);

    private async Task<bool> HasAdministrativeCapabilityAsync(
        Guid tenantId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var actor = actors.ActorContext;
        if (!actor.IsAuthenticated || actor.TenantId != tenantId) return false;
        if (!await projects.IsActorActiveTenantMemberAsync(cancellationToken).ConfigureAwait(false)) return false;
        return actor.IsSystemAdmin || actor.IsTenantAdmin || actor.HasAnyPermission(permissions);
    }
}
