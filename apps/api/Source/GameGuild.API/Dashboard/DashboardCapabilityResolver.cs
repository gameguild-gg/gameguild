using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using GameGuild.TestingLab;

namespace GameGuild.API.Dashboard;

public static class DashboardCapabilities
{
    public const string CommunityManage = "Community.Manage";
    public const string CommunityManageMembers = "Community.ManageMembers";
    public const string CommunityManageSupport = "Community.ManageSupport";
    public const string CommunityManageTeams = "Community.ManageTeams";
    public const string CommunityManageProjects = "Community.ManageProjects";
    public const string PlatformManageRoles = "Platform.ManageRoles";
    public const string LearningManage = "Learning.Manage";
    public const string TestingLabManageEvents = "TestingLab.ManageEvents";
    public const string TestingLabReviewApplications = "TestingLab.ReviewApplications";
    public const string TestingLabManageParticipants = "TestingLab.ManageParticipants";
    public const string TestingLabManageFeedback = "TestingLab.ManageFeedback";
    public const string TestingLabViewAnalytics = "TestingLab.ViewAnalytics";
    public const string TestingLabManageSettings = "TestingLab.ManageSettings";
    public const string LaunchPadManageEvents = "LaunchPad.ManageEvents";
    public const string LaunchPadReviewApplications = "LaunchPad.ReviewApplications";
    public const string LaunchPadManageParticipants = "LaunchPad.ManageParticipants";
    public const string LaunchPadViewAnalytics = "LaunchPad.ViewAnalytics";
    public const string LaunchPadManageSettings = "LaunchPad.ManageSettings";
    public const string EconomyManagePayouts = "Economy.ManagePayouts";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        CommunityManage,
        CommunityManageMembers,
        CommunityManageSupport,
        CommunityManageTeams,
        CommunityManageProjects,
        PlatformManageRoles,
        LearningManage,
        TestingLabManageEvents,
        TestingLabReviewApplications,
        TestingLabManageParticipants,
        TestingLabManageFeedback,
        TestingLabViewAnalytics,
        TestingLabManageSettings,
        LaunchPadManageEvents,
        LaunchPadReviewApplications,
        LaunchPadManageParticipants,
        LaunchPadViewAnalytics,
        LaunchPadManageSettings,
        EconomyManagePayouts,
    };
}

public static class DashboardCapabilityResolver
{
    public static IReadOnlySet<string> Resolve(
        ActorContext actor,
        IReadOnlyCollection<TestingLabUserPermission> testingLabPermissions)
    {
        if (!actor.IsAuthenticated) return new HashSet<string>();
        if (actor.IsTenantAdmin)
            return new HashSet<string>(DashboardCapabilities.All, StringComparer.Ordinal);

        var capabilities = new HashSet<string>(StringComparer.Ordinal);

        AddActorCapabilities(actor, capabilities);
        AddTestingLabCapabilities(testingLabPermissions, capabilities);

        return capabilities;
    }

    private static void AddActorCapabilities(ActorContext actor, ISet<string> capabilities)
    {
        if (actor.HasAnyPermission("users:read", "users:update", "users:roles", "groups:read", "groups:update"))
        {
            capabilities.Add(DashboardCapabilities.CommunityManage);
            capabilities.Add(DashboardCapabilities.CommunityManageMembers);
        }

        if (actor.HasAnyPermission("support:read", "support:update", "tickets:read", "tickets:update"))
            capabilities.Add(DashboardCapabilities.CommunityManageSupport);

        if (actor.HasPermission(TeamPermission.Keys.Admin))
            capabilities.Add(DashboardCapabilities.CommunityManageTeams);

        if (actor.HasPermission(ProjectPermission.Keys.Admin))
            capabilities.Add(DashboardCapabilities.CommunityManageProjects);

        if (actor.HasPermission(WalletsPermission.Keys.Admin))
            capabilities.Add(DashboardCapabilities.EconomyManagePayouts);

        if (actor.HasAnyPermission("roles:read", "roles:create", "roles:update", "roles:delete", "roles:assign"))
            capabilities.Add(DashboardCapabilities.PlatformManageRoles);

        if (actor.Permissions.Any(permission =>
                permission.StartsWith("courses:", StringComparison.Ordinal) ||
                permission.StartsWith("tutorials:", StringComparison.Ordinal) ||
                permission.StartsWith("resources:", StringComparison.Ordinal)))
            capabilities.Add(DashboardCapabilities.LearningManage);

        AddIfAny(actor, capabilities, DashboardCapabilities.LaunchPadManageEvents,
            "launchpad:events:create", "launchpad:events:update", "launchpad:events:delete", "launchpad:events:manage");
        AddIfAny(actor, capabilities, DashboardCapabilities.LaunchPadReviewApplications,
            "launchpad:applications:read", "launchpad:applications:review", "launchpad:applications:manage");
        AddIfAny(actor, capabilities, DashboardCapabilities.LaunchPadManageParticipants,
            "launchpad:participants:read", "launchpad:participants:manage");
        AddIfAny(actor, capabilities, DashboardCapabilities.LaunchPadViewAnalytics,
            "launchpad:analytics:read");
        AddIfAny(actor, capabilities, DashboardCapabilities.LaunchPadManageSettings,
            "launchpad:settings:manage");
    }

    private static void AddTestingLabCapabilities(
        IEnumerable<TestingLabUserPermission> permissions,
        ISet<string> capabilities)
    {
        var activeCollectionPermissions = permissions.Where(permission =>
            permission.ResourceId is null &&
            (!permission.ExpiresAt.HasValue || permission.ExpiresAt.Value > DateTime.UtcNow));

        foreach (var permission in activeCollectionPermissions)
        {
            if (Matches(permission, TestingLabResourceTypes.Event,
                    TestingLabActions.Create, TestingLabActions.Edit, TestingLabActions.Delete, TestingLabActions.Manage))
                capabilities.Add(DashboardCapabilities.TestingLabManageEvents);

            if (Matches(permission, TestingLabResourceTypes.Application,
                    TestingLabActions.Read, TestingLabActions.Edit, TestingLabActions.Approve, TestingLabActions.Manage))
                capabilities.Add(DashboardCapabilities.TestingLabReviewApplications);

            if (Matches(permission, TestingLabResourceTypes.Participant,
                    TestingLabActions.Manage, TestingLabActions.Edit))
                capabilities.Add(DashboardCapabilities.TestingLabManageParticipants);

            if (Matches(permission, TestingLabResourceTypes.Feedback,
                    TestingLabActions.Moderate, TestingLabActions.Edit, TestingLabActions.Delete))
                capabilities.Add(DashboardCapabilities.TestingLabManageFeedback);

            if (Matches(permission, TestingLabResourceTypes.Analytics, TestingLabActions.Read))
                capabilities.Add(DashboardCapabilities.TestingLabViewAnalytics);

            if (Matches(permission, TestingLabResourceTypes.Location,
                    TestingLabActions.Create, TestingLabActions.Edit, TestingLabActions.Delete))
                capabilities.Add(DashboardCapabilities.TestingLabManageSettings);

            if (Matches(permission, TestingLabResourceTypes.Settings,
                    TestingLabActions.Read, TestingLabActions.Edit, TestingLabActions.Manage))
                capabilities.Add(DashboardCapabilities.TestingLabManageSettings);
        }
    }

    private static void AddIfAny(
        ActorContext actor,
        ISet<string> capabilities,
        string capability,
        params string[] permissions)
    {
        if (actor.HasAnyPermission(permissions)) capabilities.Add(capability);
    }

    private static bool Matches(
        TestingLabUserPermission permission,
        string resourceType,
        params string[] actions) =>
        string.Equals(permission.ResourceType, resourceType, StringComparison.OrdinalIgnoreCase) &&
        actions.Contains(permission.Action, StringComparer.OrdinalIgnoreCase);
}
