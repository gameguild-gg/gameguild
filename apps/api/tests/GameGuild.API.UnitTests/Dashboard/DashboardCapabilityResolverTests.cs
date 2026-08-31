using FluentAssertions;
using GameGuild.API.Dashboard;
using GameGuild.Identity.Authorization;
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

        result.Should().BeEquivalentTo(
            DashboardCapabilities.EconomyManagePayouts,
            DashboardCapabilities.EconomyReviewPayouts,
            DashboardCapabilities.EconomyOperatePayouts);
    }

    [Fact]
    public void Resolve_MapsEachEconomyPermissionToItsOwnCapability()
    {
        var mappings = new Dictionary<string, string>
        {
            [EconomyPermission.Keys.ReadOperations] = DashboardCapabilities.EconomyReadOperations,
            [EconomyPermission.Keys.ReviewPayouts] = DashboardCapabilities.EconomyReviewPayouts,
            [EconomyPermission.Keys.OperatePayouts] = DashboardCapabilities.EconomyOperatePayouts,
            [EconomyPermission.Keys.OperateCompliance] = DashboardCapabilities.EconomyOperateCompliance,
            [EconomyPermission.Keys.ManagePolicies] = DashboardCapabilities.EconomyManagePolicies,
            [EconomyPermission.Keys.ManageReserves] = DashboardCapabilities.EconomyManageReserves,
            [EconomyPermission.Keys.OperateLedger] = DashboardCapabilities.EconomyOperateLedger,
            [EconomyPermission.Keys.ManageKillSwitches] = DashboardCapabilities.EconomyManageKillSwitches,
            [EconomyPermission.Keys.OperateAdRewards] = DashboardCapabilities.EconomyOperateAdRewards,
            [EconomyPermission.Keys.OperateMarketplace] = DashboardCapabilities.EconomyOperateMarketplace,
            [EconomyPermission.Keys.OperateBounties] = DashboardCapabilities.EconomyOperateBounties,
            [EconomyPermission.Keys.OperateTreasury] = DashboardCapabilities.EconomyOperateTreasury,
            [EconomyPermission.Keys.ManageLegacyMigration] = DashboardCapabilities.EconomyManageLegacyMigration,
        };

        foreach (var (permission, capability) in mappings)
        {
            var actor = Actor(roles: Set("Member"), permissions: Set(permission));

            DashboardCapabilityResolver.Resolve(actor, []).Should().BeEquivalentTo(capability);
        }
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
