using FluentAssertions;
using GameGuild.API.Dashboard;
using GameGuild.Identity.Context.Actors;
using GameGuild.TestingLab;

namespace GameGuild.API.UnitTests.Dashboard;

public sealed class DashboardCapabilityResolverTests
{
    [Fact]
    public void Resolve_DoesNotExposeManagementForRegularMember()
    {
        var actor = Actor(roles: Set("Member"));

        var result = DashboardCapabilityResolver.Resolve(actor, []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_MapsOnlyCollectionTestingLabManagementPermissions()
    {
        var actor = Actor(roles: Set("Member"));
        var permissions = new[]
        {
            Permission(TestingLabActions.Create, TestingLabResourceTypes.Event),
            Permission(TestingLabActions.Manage, TestingLabResourceTypes.Participant),
            Permission(TestingLabActions.Moderate, TestingLabResourceTypes.Feedback),
            Permission(TestingLabActions.Approve, TestingLabResourceTypes.Application, Guid.NewGuid()),
            Permission(TestingLabActions.Create, TestingLabResourceTypes.Request),
        };

        var result = DashboardCapabilityResolver.Resolve(actor, permissions);

        result.Should().BeEquivalentTo(
            DashboardCapabilities.TestingLabManageEvents,
            DashboardCapabilities.TestingLabManageParticipants,
            DashboardCapabilities.TestingLabManageFeedback);
    }

    [Fact]
    public void Resolve_MapsPlatformPermissionsWithoutGrantingUnrelatedModules()
    {
        var actor = Actor(
            roles: Set("Member"),
            permissions: Set("roles:read", "courses:update"));

        var result = DashboardCapabilityResolver.Resolve(actor, []);

        result.Should().BeEquivalentTo(
            DashboardCapabilities.PlatformManageRoles,
            DashboardCapabilities.LearningManage);
    }

    [Fact]
    public void Resolve_MapsWalletAdministrationToEconomyPayoutManagement()
    {
        var actor = Actor(
            roles: Set("Member"),
            permissions: Set("wallets:admin"));

        var result = DashboardCapabilityResolver.Resolve(actor, []);

        result.Should().BeEquivalentTo(DashboardCapabilities.EconomyManagePayouts);
    }

    [Fact]
    public void Resolve_GrantsAllManagementCapabilitiesToTenantAdmin()
    {
        var actor = Actor(roles: Set("TenantAdmin"));

        var result = DashboardCapabilityResolver.Resolve(actor, []);

        result.Should().BeEquivalentTo(DashboardCapabilities.All);
    }

    private static TestingLabUserPermission Permission(
        string action,
        string resourceType,
        Guid? resourceId = null) => new()
        {
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
        };

    private static ActorContext Actor(
        IReadOnlySet<string> roles,
        IReadOnlySet<string>? permissions = null) => new()
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid(),
            Roles = roles,
            Permissions = permissions ?? new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true,
        };

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);
}
