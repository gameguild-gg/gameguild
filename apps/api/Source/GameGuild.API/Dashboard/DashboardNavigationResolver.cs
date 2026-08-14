namespace GameGuild.API.Dashboard;

public sealed record DashboardNavigationItem(
    string Title,
    string? Route,
    IReadOnlyList<DashboardNavigationItem> Children);

public sealed record DashboardNavigationGroup(
    string Label,
    IReadOnlyList<DashboardNavigationItem> Items);

public static class DashboardNavigationResolver
{
    public static IReadOnlyList<DashboardNavigationGroup> Resolve(IReadOnlyCollection<string> capabilities)
    {
        var allowed = capabilities.ToHashSet(StringComparer.Ordinal);
        var groups = new List<DashboardNavigationGroup>
        {
            new("Overview",
            [
                Item("Dashboard", "/dashboard"),
                Item("Invitations", "/dashboard/invitations"),
            ]),
        };

        var community = new List<DashboardNavigationItem>();
        if (allowed.Contains(DashboardCapabilities.CommunityManage))
            community.Add(Item("Overview", "/dashboard/community"));

        var memberItems = new List<DashboardNavigationItem>();
        if (allowed.Contains(DashboardCapabilities.CommunityManageMembers))
        {
            memberItems.Add(Item("Overview", "/dashboard/community/members"));
            memberItems.Add(Item("Users", "/dashboard/community/members/users"));
            memberItems.Add(Item("Groups", "/dashboard/community/members/groups"));
        }
        if (allowed.Contains(DashboardCapabilities.CommunityManageSupport))
            memberItems.Add(Item("Support", "/dashboard/community/members/support"));
        if (memberItems.Count > 0)
            community.Add(new DashboardNavigationItem("Members", null, memberItems));

        var testingLab = Children(allowed,
            ("Events", "/dashboard/testing-lab/events", DashboardCapabilities.TestingLabManageEvents),
            ("Applications", "/dashboard/testing-lab/applications", DashboardCapabilities.TestingLabReviewApplications),
            ("Projects", "/dashboard/testing-lab/projects", DashboardCapabilities.TestingLabReviewApplications),
            ("Participants", "/dashboard/testing-lab/participants", DashboardCapabilities.TestingLabManageParticipants),
            ("Feedback", "/dashboard/testing-lab/feedback", DashboardCapabilities.TestingLabManageFeedback),
            ("Analytics", "/dashboard/testing-lab/analytics", DashboardCapabilities.TestingLabViewAnalytics),
            ("Locations", "/dashboard/testing-lab/locations", DashboardCapabilities.TestingLabManageSettings),
            ("Access", "/dashboard/testing-lab/access", DashboardCapabilities.TestingLabManageSettings),
            ("Settings", "/dashboard/testing-lab/settings", DashboardCapabilities.TestingLabManageSettings));
        if (testingLab.Count > 0)
        {
            testingLab.Insert(0, Item("Overview", "/dashboard/testing-lab"));
            community.Add(new DashboardNavigationItem("Testing Lab", null, testingLab));
        }

        var launchPad = Children(allowed,
            ("Events", "/dashboard/launch-pad/events", DashboardCapabilities.LaunchPadManageEvents),
            ("Applications", "/dashboard/launch-pad/applications", DashboardCapabilities.LaunchPadReviewApplications),
            ("Participants", "/dashboard/launch-pad/participants", DashboardCapabilities.LaunchPadManageParticipants),
            ("Analytics", "/dashboard/launch-pad/analytics", DashboardCapabilities.LaunchPadViewAnalytics),
            ("Settings", "/dashboard/launch-pad/settings", DashboardCapabilities.LaunchPadManageSettings));
        if (launchPad.Count > 0)
        {
            launchPad.Insert(0, Item("Overview", "/dashboard/launch-pad"));
            community.Add(new DashboardNavigationItem("Launch Pad", null, launchPad));
        }

        if (community.Count > 0)
            groups.Add(new DashboardNavigationGroup("Community Management", community));

        var platform = new List<DashboardNavigationItem>();
        if (allowed.Contains(DashboardCapabilities.PlatformManageRoles))
            platform.Add(Item("Roles", "/dashboard/platform/roles"));
        if (allowed.Contains(DashboardCapabilities.LearningManage))
        {
            platform.Add(new DashboardNavigationItem("Learning", null,
            [
                Item("Overview", "/dashboard/learning"),
                Item("Courses", "/dashboard/learning/courses"),
                Item("Tutorials", "/dashboard/learning/tutorials"),
                Item("Resources", "/dashboard/learning/resources"),
            ]));
        }
        if (platform.Count > 0)
            groups.Add(new DashboardNavigationGroup("Platform Management", platform));

        return groups;
    }

    private static List<DashboardNavigationItem> Children(
        IReadOnlySet<string> capabilities,
        params (string Title, string Route, string Capability)[] definitions) => definitions
        .Where(definition => capabilities.Contains(definition.Capability))
        .Select(definition => Item(definition.Title, definition.Route))
        .ToList();

    private static DashboardNavigationItem Item(string title, string route) => new(title, route, []);
}
