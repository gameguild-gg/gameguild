using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Social.Groups.UnitTests;

public class SocialGroupTests
{
    [Fact]
    public void Controller_ShouldProtectAdministrativeGroupOperations()
    {
        var mutationNames = new[]
        {
            nameof(SocialGroupsController.Create),
            nameof(SocialGroupsController.Update),
            nameof(SocialGroupsController.Activate),
            nameof(SocialGroupsController.Archive),
            nameof(SocialGroupsController.Suspend),
            nameof(SocialGroupsController.ListMembers),
            nameof(SocialGroupsController.Join),
            nameof(SocialGroupsController.Approve),
            nameof(SocialGroupsController.Reject),
            nameof(SocialGroupsController.ChangeRole),
            nameof(SocialGroupsController.Leave)
        };

        foreach (var methodName in mutationNames)
        {
            var authorize = typeof(SocialGroupsController)
                .GetMethod(methodName)!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .Should()
                .ContainSingle()
                .Subject;

            authorize.Policy.Should().Be(Policies.TenantAdmin);
        }
    }

    [Fact]
    public async Task Controller_Create_DerivesOwnerAndTenantFromAuthenticatedActor()
    {
        var sender = new StubSender();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        sender.Next = new SocialGroupDto(Guid.NewGuid(), tenantId, actorId, "Group", "group", null, SocialGroupType.StudyGroup, SocialGroupVisibility.Private, SocialGroupStatus.Active, 1, 0, DateTime.UtcNow, DateTime.UtcNow);
        var controller = CreateController(sender, CreateActorContext(actorId, tenantId, "TenantAdmin"));

        await controller.Create(
            new CreateSocialGroupRequest(Guid.NewGuid(), "Group", "group", SocialGroupType.StudyGroup, SocialGroupVisibility.Private, TenantId: Guid.NewGuid()),
            CancellationToken.None);

        sender.LastRequest.Should().BeOfType<CreateSocialGroupCommand>().Which.Should().Match<CreateSocialGroupCommand>(command =>
            command.OwnerId == actorId && command.TenantId == tenantId);
    }

    [Fact]
    public async Task Controller_List_AnonymousCallOnlyQueriesPublicActiveGroups()
    {
        var sender = new StubSender { Next = new List<SocialGroupDto>() };
        var controller = CreateController(sender, ActorContext.Anonymous);

        await controller.List(null, null, null, null, null, null, 0, 25, CancellationToken.None);

        sender.LastRequest.Should().BeOfType<ListSocialGroupsQuery>().Which.Should().Match<ListSocialGroupsQuery>(query =>
            query.Visibility == SocialGroupVisibility.Public && query.Status == SocialGroupStatus.Active);
    }

    [Fact]
    public async Task Controller_Get_AnonymousCallDoesNotExposePrivateGroup()
    {
        var sender = new StubSender
        {
            Next = new SocialGroupDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Private", "private", null, SocialGroupType.StudyGroup, SocialGroupVisibility.Private, SocialGroupStatus.Active, 1, 0, DateTime.UtcNow, DateTime.UtcNow)
        };
        var controller = CreateController(sender, ActorContext.Anonymous);

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Controller_Update_ForbidsTenantAdminFromMutatingAnotherTenantGroup()
    {
        var actorTenantId = Guid.NewGuid();
        var sender = new StubSender
        {
            Next = new SocialGroupDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Other", "other", null, SocialGroupType.StudyGroup, SocialGroupVisibility.Private, SocialGroupStatus.Active, 1, 0, DateTime.UtcNow, DateTime.UtcNow)
        };
        var controller = CreateController(sender, CreateActorContext(Guid.NewGuid(), actorTenantId, "TenantAdmin"));

        var result = await controller.Update(Guid.NewGuid(), new UpdateSocialGroupRequest("Changed", "changed", SocialGroupType.StudyGroup, SocialGroupVisibility.Private), CancellationToken.None);

        result.Result.Should().BeOfType<ForbidResult>();
        sender.Requests.Should().NotContain(request => request is UpdateSocialGroupCommand);
    }

    internal static SocialGroupsController CreateController(StubSender sender, ActorContext actor)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(actor);
        return new SocialGroupsController(sender, accessor)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    internal static ActorContext CreateActorContext(Guid userId, Guid? tenantId, params string[] roles)
        => new()
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Roles = roles.ToHashSet(StringComparer.OrdinalIgnoreCase),
            Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        };

    [Fact]
    public void Create_SetsDefaultsAndNormalizesValues()
    {
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var group = SocialGroup.Create(
            ownerId,
            "  Advanced AI Study Circle  ",
            "  Advanced-AI  ",
            SocialGroupType.StudyGroup,
            SocialGroupVisibility.Private,
            "  Weekly peer learning  ",
            tenantId);

        group.OwnerId.Should().Be(ownerId);
        group.TenantId.Should().Be(tenantId);
        group.Name.Should().Be("Advanced AI Study Circle");
        group.Slug.Should().Be("advanced-ai");
        group.Description.Should().Be("Weekly peer learning");
        group.Type.Should().Be(SocialGroupType.StudyGroup);
        group.Visibility.Should().Be(SocialGroupVisibility.Private);
        group.Status.Should().Be(SocialGroupStatus.Active);
        group.MemberCount.Should().Be(1);
        group.PendingMemberCount.Should().Be(0);
    }

    [Fact]
    public void Create_RejectsEmptyRequiredFields()
    {
        Action emptyName = () => SocialGroup.Create(Guid.NewGuid(), " ", "slug", SocialGroupType.ProjectTeam, SocialGroupVisibility.Public);
        Action emptySlug = () => SocialGroup.Create(Guid.NewGuid(), "Name", " ", SocialGroupType.ProjectTeam, SocialGroupVisibility.Public);

        emptyName.Should().Throw<ArgumentException>();
        emptySlug.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_ChangesMutableFields()
    {
        var group = SocialGroup.Create(Guid.NewGuid(), "Original", "original", SocialGroupType.StudyGroup, SocialGroupVisibility.Public);

        group.UpdateDetails("New Name", "New-Slug", null, SocialGroupType.GameJamTeam, SocialGroupVisibility.InviteOnly);

        group.Name.Should().Be("New Name");
        group.Slug.Should().Be("new-slug");
        group.Description.Should().BeNull();
        group.Type.Should().Be(SocialGroupType.GameJamTeam);
        group.Visibility.Should().Be(SocialGroupVisibility.InviteOnly);
    }

    [Fact]
    public void CreateAndUpdate_TruncateLongValuesAndNormalizeEmptyDescription()
    {
        var longName = new string('n', 140);
        var longSlug = new string('s', 180);
        var longDescription = new string('d', 1_100);

        var group = SocialGroup.Create(Guid.NewGuid(), longName, longSlug, SocialGroupType.CourseCohort, SocialGroupVisibility.InviteOnly, longDescription);
        group.UpdateDetails(longName, longSlug.ToUpperInvariant(), "   ", SocialGroupType.Institution, SocialGroupVisibility.Private);

        group.Name.Should().HaveLength(120);
        group.Slug.Should().HaveLength(160);
        group.Slug.Should().Be(group.Slug.ToLowerInvariant());
        group.Description.Should().BeNull();
    }

    [Fact]
    public void StatusTransitions_SetExpectedStatus()
    {
        var group = SocialGroup.Create(Guid.NewGuid(), "Group", "group", SocialGroupType.InterestCommunity, SocialGroupVisibility.Public);

        group.Archive();
        group.Status.Should().Be(SocialGroupStatus.Archived);

        group.Suspend();
        group.Status.Should().Be(SocialGroupStatus.Suspended);

        group.Activate();
        group.Status.Should().Be(SocialGroupStatus.Active);
    }

    [Fact]
    public void MembershipCounters_DoNotUnderflow()
    {
        var group = SocialGroup.Create(Guid.NewGuid(), "Group", "group", SocialGroupType.InterestCommunity, SocialGroupVisibility.Public);

        group.RecordMembershipRequested();
        group.PendingMemberCount.Should().Be(1);

        group.RecordMembershipRejected();
        group.PendingMemberCount.Should().Be(0);

        group.RecordMembershipRejected();
        group.PendingMemberCount.Should().Be(0);

        group.RecordMembershipRemoved(SocialGroupMembershipStatus.Active);
        group.MemberCount.Should().Be(0);

        group.RecordMembershipRemoved(SocialGroupMembershipStatus.Active);
        group.MemberCount.Should().Be(0);

        group.RecordMembershipApproved();
        group.MemberCount.Should().Be(1);

        group.RecordMembershipRequested();
        group.RecordMembershipRemoved(SocialGroupMembershipStatus.Pending);
        group.PendingMemberCount.Should().Be(0);
    }
}

public class SocialGroupMemberTests
{
    [Fact]
    public void CreateOwner_SetsActiveOwner()
    {
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var member = SocialGroupMember.CreateOwner(groupId, userId);

        member.GroupId.Should().Be(groupId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(SocialGroupMemberRole.Owner);
        member.Status.Should().Be(SocialGroupMembershipStatus.Active);
        member.JoinedAt.Should().NotBeNull();
    }

    [Fact]
    public void Request_CreatesPendingOrActiveMembership()
    {
        var pending = SocialGroupMember.Request(Guid.NewGuid(), Guid.NewGuid(), SocialGroupMemberRole.Admin, approveImmediately: false);
        var active = SocialGroupMember.Request(Guid.NewGuid(), Guid.NewGuid(), SocialGroupMemberRole.Owner, approveImmediately: true);

        pending.Role.Should().Be(SocialGroupMemberRole.Admin);
        pending.Status.Should().Be(SocialGroupMembershipStatus.Pending);
        pending.JoinedAt.Should().BeNull();

        active.Role.Should().Be(SocialGroupMemberRole.Member);
        active.Status.Should().Be(SocialGroupMembershipStatus.Active);
        active.JoinedAt.Should().NotBeNull();
    }

    [Fact]
    public void MembershipLifecycle_ApprovesRejectsChangesRoleAndRemoves()
    {
        var member = SocialGroupMember.Request(Guid.NewGuid(), Guid.NewGuid(), SocialGroupMemberRole.Member, approveImmediately: false);
        var approverId = Guid.NewGuid();

        member.Approve(approverId);
        member.Status.Should().Be(SocialGroupMembershipStatus.Active);
        member.ApprovedByUserId.Should().Be(approverId);

        member.ChangeRole(SocialGroupMemberRole.Moderator);
        member.Role.Should().Be(SocialGroupMemberRole.Moderator);

        member.Remove();
        member.Status.Should().Be(SocialGroupMembershipStatus.Removed);
        member.RemovedAt.Should().NotBeNull();

        var rejected = SocialGroupMember.Request(Guid.NewGuid(), Guid.NewGuid(), SocialGroupMemberRole.Member, approveImmediately: false);
        rejected.Reject();
        rejected.Status.Should().Be(SocialGroupMembershipStatus.Rejected);

        rejected.RequestAgain(SocialGroupMemberRole.Owner, approveImmediately: true);
        rejected.Status.Should().Be(SocialGroupMembershipStatus.Active);
        rejected.Role.Should().Be(SocialGroupMemberRole.Member);
        rejected.RemovedAt.Should().BeNull();
    }

    [Fact]
    public void ChangeRole_RejectsOwnerRole()
    {
        var member = SocialGroupMember.Request(Guid.NewGuid(), Guid.NewGuid(), SocialGroupMemberRole.Member, approveImmediately: true);

        Action action = () => member.ChangeRole(SocialGroupMemberRole.Owner);

        action.Should().Throw<ArgumentException>();
    }
}

public class SocialGroupServiceTests
{
    [Fact]
    public void Dtos_ExposeAllProperties()
    {
        var groupId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var group = new SocialGroupDto(
            groupId,
            tenantId,
            ownerId,
            "Name",
            "name",
            "description",
            SocialGroupType.Institution,
            SocialGroupVisibility.InviteOnly,
            SocialGroupStatus.Suspended,
            9,
            3,
            createdAt,
            updatedAt);

        group.Id.Should().Be(groupId);
        group.TenantId.Should().Be(tenantId);
        group.OwnerId.Should().Be(ownerId);
        group.Name.Should().Be("Name");
        group.Slug.Should().Be("name");
        group.Description.Should().Be("description");
        group.Type.Should().Be(SocialGroupType.Institution);
        group.Visibility.Should().Be(SocialGroupVisibility.InviteOnly);
        group.Status.Should().Be(SocialGroupStatus.Suspended);
        group.MemberCount.Should().Be(9);
        group.PendingMemberCount.Should().Be(3);
        group.CreatedAt.Should().Be(createdAt);
        group.UpdatedAt.Should().Be(updatedAt);

        var memberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestedAt = DateTime.UtcNow.AddHours(-2);
        var joinedAt = DateTime.UtcNow.AddHours(-1);
        var approvedBy = Guid.NewGuid();
        var removedAt = DateTime.UtcNow;
        var member = new SocialGroupMemberDto(memberId, groupId, userId, SocialGroupMemberRole.Moderator, SocialGroupMembershipStatus.Removed, requestedAt, joinedAt, approvedBy, removedAt);

        member.Id.Should().Be(memberId);
        member.GroupId.Should().Be(groupId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(SocialGroupMemberRole.Moderator);
        member.Status.Should().Be(SocialGroupMembershipStatus.Removed);
        member.RequestedAt.Should().Be(requestedAt);
        member.JoinedAt.Should().Be(joinedAt);
        member.ApprovedByUserId.Should().Be(approvedBy);
        member.RemovedAt.Should().Be(removedAt);
    }

    [Fact]
    public async Task Create_AddsGroupAndOwnerMembership()
    {
        var groupRepository = new InMemorySocialGroupRepository();
        var memberRepository = new InMemorySocialGroupMemberRepository();
        var service = new SocialGroupService(groupRepository, memberRepository);
        var ownerId = Guid.NewGuid();

        var group = await service.CreateAsync(new CreateSocialGroupCommand(
            ownerId,
            "Study Group",
            "study-group",
            SocialGroupType.StudyGroup,
            SocialGroupVisibility.Public,
            null,
            null));

        group.OwnerId.Should().Be(ownerId);
        group.MemberCount.Should().Be(1);
        memberRepository.Members.Should().ContainSingle(member => member.GroupId == group.Id && member.Role == SocialGroupMemberRole.Owner);
    }

    [Fact]
    public async Task Join_PublicGroup_ActivatesImmediately()
    {
        var (service, groups, _) = CreateSeededService(SocialGroupVisibility.Public);
        var group = groups.Groups.Single();
        var userId = Guid.NewGuid();

        var membership = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Member));

        membership.Should().NotBeNull();
        membership!.Status.Should().Be(SocialGroupMembershipStatus.Active);
        group.MemberCount.Should().Be(2);
        group.PendingMemberCount.Should().Be(0);
    }

    [Fact]
    public async Task Join_PrivateGroup_QueuesAndApprovePromotes()
    {
        var (service, groups, _) = CreateSeededService(SocialGroupVisibility.Private);
        var group = groups.Groups.Single();
        var userId = Guid.NewGuid();

        var membership = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Member));
        var approved = await service.ApproveMemberAsync(group.Id, userId, Guid.NewGuid());

        membership!.Status.Should().Be(SocialGroupMembershipStatus.Pending);
        approved.Should().BeTrue();
        group.MemberCount.Should().Be(2);
        group.PendingMemberCount.Should().Be(0);
    }

    [Fact]
    public async Task Join_MissingOrSuspendedGroup_ReturnsNull()
    {
        var (service, groups, _) = CreateSeededService(SocialGroupVisibility.Public);
        var group = groups.Groups.Single();
        group.Suspend();

        var suspended = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, Guid.NewGuid(), SocialGroupMemberRole.Member));
        var missing = await service.JoinAsync(new JoinSocialGroupCommand(Guid.NewGuid(), Guid.NewGuid(), SocialGroupMemberRole.Member));

        suspended.Should().BeNull();
        missing.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateJoin_ReturnsExistingMembership()
    {
        var (service, groups, _) = CreateSeededService(SocialGroupVisibility.Public);
        var group = groups.Groups.Single();
        var userId = Guid.NewGuid();

        var first = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Member));
        var second = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Admin));

        first!.Id.Should().Be(second!.Id);
        group.MemberCount.Should().Be(2);
    }

    [Fact]
    public async Task RejoinRejectedMembership_ReusesExistingRowAndUpdatesCounters()
    {
        var (service, groups, members) = CreateSeededService(SocialGroupVisibility.Private);
        var group = groups.Groups.Single();
        var userId = Guid.NewGuid();

        var first = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Member));
        await service.RejectMemberAsync(group.Id, userId);
        var second = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Admin));

        second!.Id.Should().Be(first!.Id);
        second.Status.Should().Be(SocialGroupMembershipStatus.Pending);
        second.Role.Should().Be(SocialGroupMemberRole.Admin);
        group.PendingMemberCount.Should().Be(1);
        members.Members.Should().ContainSingle(member => member.GroupId == group.Id && member.UserId == userId);
    }

    [Fact]
    public async Task RejoinRemovedPublicMembership_ReusesExistingRowAndActivatesImmediately()
    {
        var (service, groups, members) = CreateSeededService(SocialGroupVisibility.Public);
        var group = groups.Groups.Single();
        var userId = Guid.NewGuid();

        var first = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Member));
        await service.LeaveAsync(group.Id, userId);
        var second = await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Moderator));

        second!.Id.Should().Be(first!.Id);
        second.Status.Should().Be(SocialGroupMembershipStatus.Active);
        second.Role.Should().Be(SocialGroupMemberRole.Moderator);
        group.MemberCount.Should().Be(2);
        members.Members.Should().ContainSingle(member => member.GroupId == group.Id && member.UserId == userId);
    }

    [Fact]
    public async Task RejectChangeRoleLeave_CoverLifecycleBranches()
    {
        var (service, groups, members) = CreateSeededService(SocialGroupVisibility.Private);
        var group = groups.Groups.Single();
        var userId = Guid.NewGuid();
        await service.JoinAsync(new JoinSocialGroupCommand(group.Id, userId, SocialGroupMemberRole.Member));

        (await service.RejectMemberAsync(group.Id, userId)).Should().BeTrue();
        group.PendingMemberCount.Should().Be(0);

        var activeUserId = Guid.NewGuid();
        members.Members.Add(SocialGroupMember.Request(group.Id, activeUserId, SocialGroupMemberRole.Member, approveImmediately: true));
        group.RecordMembershipActivated();

        (await service.ChangeRoleAsync(group.Id, activeUserId, SocialGroupMemberRole.Admin)).Should().BeTrue();
        (await service.LeaveAsync(group.Id, activeUserId)).Should().BeTrue();
        group.MemberCount.Should().Be(1);

        (await service.ChangeRoleAsync(group.Id, group.OwnerId, SocialGroupMemberRole.Admin)).Should().BeFalse();
        (await service.LeaveAsync(group.Id, group.OwnerId)).Should().BeFalse();
    }

    [Fact]
    public async Task ListGetUpdateStatusAndMembers_ReturnExpectedResults()
    {
        var (service, groups, _) = CreateSeededService(SocialGroupVisibility.Public);
        var group = groups.Groups.Single();

        var updated = await service.UpdateAsync(new UpdateSocialGroupCommand(
            group.Id,
            "Renamed",
            "renamed",
            SocialGroupType.GameJamTeam,
            SocialGroupVisibility.InviteOnly,
            "updated"));
        var listed = await service.ListAsync(new ListSocialGroupsQuery(Search: "ren", Take: 10));
        var members = await service.ListMembersAsync(new ListSocialGroupMembersQuery(group.Id));
        var archived = await service.SetStatusAsync(group.Id, SocialGroupStatus.Archived);

        updated!.Name.Should().Be("Renamed");
        listed.Should().ContainSingle(item => item.Id == group.Id);
        members.Should().ContainSingle(member => member.Role == SocialGroupMemberRole.Owner);
        archived.Should().BeTrue();
        (await service.GetAsync(Guid.NewGuid())).Should().BeNull();
        (await service.UpdateAsync(new UpdateSocialGroupCommand(Guid.NewGuid(), "n", "n", SocialGroupType.StudyGroup, SocialGroupVisibility.Public, null))).Should().BeNull();
        (await service.SetStatusAsync(Guid.NewGuid(), SocialGroupStatus.Active)).Should().BeFalse();
        await service.SetStatusAsync(group.Id, SocialGroupStatus.Suspended);
        await service.SetStatusAsync(group.Id, SocialGroupStatus.Active);
        await service.Invoking(instance => instance.SetStatusAsync(group.Id, (SocialGroupStatus)999)).Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Controller_UsesSenderAndMapsNotFoundResponses()
    {
        var sender = new StubSender();
        var controller = SocialGroupTests.CreateController(sender, SocialGroupTests.CreateActorContext(Guid.NewGuid(), Guid.NewGuid(), "SystemAdmin"));
        var groupId = Guid.NewGuid();
        var group = new SocialGroupDto(groupId, null, Guid.NewGuid(), "Group", "group", null, SocialGroupType.StudyGroup, SocialGroupVisibility.Public, SocialGroupStatus.Active, 1, 0, DateTime.UtcNow, DateTime.UtcNow);
        var member = new SocialGroupMemberDto(Guid.NewGuid(), groupId, Guid.NewGuid(), SocialGroupMemberRole.Member, SocialGroupMembershipStatus.Active, DateTime.UtcNow, DateTime.UtcNow, null, null);

        sender.SetResponse<GetSocialGroupQuery>(group);
        sender.SetResponse<UpdateSocialGroupCommand>(group);
        (await controller.Get(groupId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.Update(groupId, new UpdateSocialGroupRequest("n", "n", SocialGroupType.StudyGroup, SocialGroupVisibility.Public), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();

        sender.SetResponse<JoinSocialGroupCommand>(member);
        (await controller.Join(groupId, new JoinSocialGroupRequest(Guid.NewGuid()), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();

        sender.SetResponse<ListSocialGroupsQuery>(new List<SocialGroupDto>());
        (await controller.List(null, null, null, null, null, null, 0, 0, CancellationToken.None)).Should().BeEmpty();
        (await controller.List(null, null, null, null, null, null, 0, 5, CancellationToken.None)).Should().BeEmpty();

        sender.SetResponse<ListSocialGroupMembersQuery>(new List<SocialGroupMemberDto>());
        (await controller.ListMembers(groupId, null, 0, 0, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.ListMembers(groupId, null, 0, 5, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();

        sender.SetResponse<GetSocialGroupQuery>(null);
        (await controller.Get(groupId, CancellationToken.None)).Result.Should().BeOfType<NotFoundResult>();
        (await controller.Update(groupId, new UpdateSocialGroupRequest("n", "n", SocialGroupType.StudyGroup, SocialGroupVisibility.Public), CancellationToken.None)).Result.Should().BeOfType<NotFoundResult>();
        (await controller.Join(groupId, new JoinSocialGroupRequest(Guid.NewGuid()), CancellationToken.None)).Result.Should().BeOfType<NotFoundResult>();

        var createdGroup = group with { Name = "Created", Slug = "created" };
        sender.SetResponse<CreateSocialGroupCommand>(createdGroup);
        var created = await controller.Create(new CreateSocialGroupRequest(Guid.NewGuid(), "Created", "created", SocialGroupType.StudyGroup, SocialGroupVisibility.Public), CancellationToken.None);
        created.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().Be(createdGroup);

        sender.SetResponse<GetSocialGroupQuery>(group);
        sender.SetResponse<SetSocialGroupStatusCommand>(true);
        sender.SetResponse<ApproveSocialGroupMemberCommand>(true);
        sender.SetResponse<RejectSocialGroupMemberCommand>(true);
        sender.SetResponse<ChangeSocialGroupMemberRoleCommand>(true);
        sender.SetResponse<LeaveSocialGroupCommand>(true);
        (await controller.Activate(groupId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.Archive(groupId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.Suspend(groupId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.Approve(groupId, Guid.NewGuid(), new ApproveSocialGroupMemberRequest(Guid.NewGuid()), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.Reject(groupId, Guid.NewGuid(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ChangeRole(groupId, Guid.NewGuid(), new ChangeSocialGroupMemberRoleRequest(SocialGroupMemberRole.Admin), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.Leave(groupId, Guid.NewGuid(), CancellationToken.None)).Should().BeOfType<NoContentResult>();

        sender.SetResponse<SetSocialGroupStatusCommand>(false);
        sender.SetResponse<ApproveSocialGroupMemberCommand>(false);
        sender.SetResponse<RejectSocialGroupMemberCommand>(false);
        sender.SetResponse<ChangeSocialGroupMemberRoleCommand>(false);
        sender.SetResponse<LeaveSocialGroupCommand>(false);
        (await controller.Activate(groupId, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.Archive(groupId, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.Suspend(groupId, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.Approve(groupId, Guid.NewGuid(), new ApproveSocialGroupMemberRequest(Guid.NewGuid()), CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.Reject(groupId, Guid.NewGuid(), CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.ChangeRole(groupId, Guid.NewGuid(), new ChangeSocialGroupMemberRoleRequest(SocialGroupMemberRole.Admin), CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.Leave(groupId, Guid.NewGuid(), CancellationToken.None)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Service_FailureBranches_ReturnFalseForInvalidMembershipStates()
    {
        var (service, groups, members) = CreateSeededService(SocialGroupVisibility.Private);
        var group = groups.Groups.Single();
        var activeUserId = Guid.NewGuid();
        var pendingUserId = Guid.NewGuid();
        members.Members.Add(SocialGroupMember.Request(group.Id, activeUserId, SocialGroupMemberRole.Member, approveImmediately: true));
        await service.JoinAsync(new JoinSocialGroupCommand(group.Id, pendingUserId, SocialGroupMemberRole.Member));

        (await service.ApproveMemberAsync(Guid.NewGuid(), activeUserId, Guid.NewGuid())).Should().BeFalse();
        (await service.ApproveMemberAsync(group.Id, activeUserId, Guid.NewGuid())).Should().BeFalse();
        (await service.RejectMemberAsync(Guid.NewGuid(), activeUserId)).Should().BeFalse();
        (await service.RejectMemberAsync(group.Id, activeUserId)).Should().BeFalse();
        (await service.ChangeRoleAsync(group.Id, Guid.NewGuid(), SocialGroupMemberRole.Admin)).Should().BeFalse();
        (await service.ChangeRoleAsync(group.Id, pendingUserId, SocialGroupMemberRole.Admin)).Should().BeFalse();
        (await service.LeaveAsync(Guid.NewGuid(), pendingUserId)).Should().BeFalse();
        (await service.LeaveAsync(group.Id, Guid.NewGuid())).Should().BeFalse();
        (await service.LeaveAsync(group.Id, pendingUserId)).Should().BeTrue();
        group.PendingMemberCount.Should().Be(0);
    }

    [Fact]
    public async Task Repositories_FilterPersistAndUpdateGroupsAndMembers()
    {
        await using var db = CreateDbContext();
        var groupRepository = new SocialGroupRepository(db);
        var memberRepository = new SocialGroupMemberRepository(db);
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var group = SocialGroup.Create(ownerId, "Study", "study", SocialGroupType.StudyGroup, SocialGroupVisibility.Public, "description", tenantId);
        var other = SocialGroup.Create(Guid.NewGuid(), "Other", "other", SocialGroupType.ProjectTeam, SocialGroupVisibility.Private);
        other.Archive();

        await groupRepository.AddAsync(group);
        await groupRepository.AddAsync(other);
        group.UpdateDetails("Study Updated", "study-updated", "updated", SocialGroupType.CourseCohort, SocialGroupVisibility.InviteOnly);
        await groupRepository.UpdateAsync(group);

        (await groupRepository.GetByIdAsync(group.Id))!.Name.Should().Be("Study Updated");
        (await groupRepository.GetBySlugAsync("STUDY-UPDATED"))!.Id.Should().Be(group.Id);
        (await groupRepository.GetBySlugAsync("missing")).Should().BeNull();
        (await groupRepository.ListAsync(new ListSocialGroupsQuery())).Should().HaveCount(2);
        (await groupRepository.ListAsync(new ListSocialGroupsQuery(tenantId, ownerId, SocialGroupType.CourseCohort, SocialGroupVisibility.InviteOnly, SocialGroupStatus.Active, "updated", -5, 500))).Should().ContainSingle(item => item.Id == group.Id);

        var active = SocialGroupMember.Request(group.Id, Guid.NewGuid(), SocialGroupMemberRole.Member, approveImmediately: true);
        var pending = SocialGroupMember.Request(group.Id, Guid.NewGuid(), SocialGroupMemberRole.Moderator, approveImmediately: false);
        await memberRepository.AddAsync(active);
        await memberRepository.AddAsync(pending);
        pending.Approve(Guid.NewGuid());
        await memberRepository.UpdateAsync(pending);

        (await memberRepository.GetByGroupUserAsync(group.Id, active.UserId))!.Status.Should().Be(SocialGroupMembershipStatus.Active);
        (await memberRepository.GetByGroupUserAsync(group.Id, Guid.NewGuid())).Should().BeNull();
        (await memberRepository.ListByGroupAsync(group.Id, null, -2, 100)).Should().HaveCount(2);
        (await memberRepository.ListByGroupAsync(group.Id, SocialGroupMembershipStatus.Active, 0, 1)).Should().ContainSingle();
    }

    [Fact]
    public async Task Handlers_DelegateToService()
    {
        var (service, groups, _) = CreateSeededService(SocialGroupVisibility.Private);
        var group = groups.Groups.Single();
        var pendingUserId = Guid.NewGuid();
        var activeUserId = Guid.NewGuid();

        (await new CreateSocialGroupCommandHandler(service).Handle(new CreateSocialGroupCommand(Guid.NewGuid(), "New", "new", SocialGroupType.ProjectTeam, SocialGroupVisibility.Public, null, null), CancellationToken.None)).Slug.Should().Be("new");
        (await new UpdateSocialGroupCommandHandler(service).Handle(new UpdateSocialGroupCommand(group.Id, "Updated", "updated", SocialGroupType.CourseCohort, SocialGroupVisibility.Private, null), CancellationToken.None))!.Name.Should().Be("Updated");
        (await new SetSocialGroupStatusCommandHandler(service).Handle(new SetSocialGroupStatusCommand(group.Id, SocialGroupStatus.Active), CancellationToken.None)).Should().BeTrue();
        (await new JoinSocialGroupCommandHandler(service).Handle(new JoinSocialGroupCommand(group.Id, pendingUserId, SocialGroupMemberRole.Member), CancellationToken.None))!.Status.Should().Be(SocialGroupMembershipStatus.Pending);
        (await new ApproveSocialGroupMemberCommandHandler(service).Handle(new ApproveSocialGroupMemberCommand(group.Id, pendingUserId, Guid.NewGuid()), CancellationToken.None)).Should().BeTrue();
        (await new JoinSocialGroupCommandHandler(service).Handle(new JoinSocialGroupCommand(group.Id, activeUserId, SocialGroupMemberRole.Member), CancellationToken.None))!.Status.Should().Be(SocialGroupMembershipStatus.Pending);
        (await new RejectSocialGroupMemberCommandHandler(service).Handle(new RejectSocialGroupMemberCommand(group.Id, activeUserId), CancellationToken.None)).Should().BeTrue();
        (await new ChangeSocialGroupMemberRoleCommandHandler(service).Handle(new ChangeSocialGroupMemberRoleCommand(group.Id, pendingUserId, SocialGroupMemberRole.Admin), CancellationToken.None)).Should().BeTrue();
        (await new LeaveSocialGroupCommandHandler(service).Handle(new LeaveSocialGroupCommand(group.Id, pendingUserId), CancellationToken.None)).Should().BeTrue();
        (await new GetSocialGroupQueryHandler(service).Handle(new GetSocialGroupQuery(group.Id), CancellationToken.None))!.Id.Should().Be(group.Id);
        (await new ListSocialGroupsQueryHandler(service).Handle(new ListSocialGroupsQuery(), CancellationToken.None)).Should().NotBeEmpty();
        (await new ListSocialGroupMembersQueryHandler(service).Handle(new ListSocialGroupMembersQuery(group.Id), CancellationToken.None)).Should().NotBeEmpty();
    }

    [Fact]
    public void DependencyInjection_ModelConfigurationAndModule_RegisterExpectedServices()
    {
        using var db = CreateDbContext();
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(db);
        services.AddSocialGroupsModule();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ISocialGroupRepository>().Should().BeOfType<SocialGroupRepository>();
        provider.GetRequiredService<ISocialGroupMemberRepository>().Should().BeOfType<SocialGroupMemberRepository>();
        provider.GetRequiredService<ISocialGroupService>().Should().BeOfType<SocialGroupService>();
        provider.GetRequiredService<IRequestHandler<CreateSocialGroupCommand, SocialGroupDto>>().Should().BeAssignableTo<ICommandHandler<CreateSocialGroupCommand, SocialGroupDto>>();
        provider.GetRequiredService<IRequestHandler<GetSocialGroupQuery, SocialGroupDto?>>().Should().BeAssignableTo<IQueryHandler<GetSocialGroupQuery, SocialGroupDto?>>();

        var modelBuilder = new ModelBuilder();
        new SocialGroupsModelConfiguration().Configure(modelBuilder);
        modelBuilder.Model.FindEntityType(typeof(SocialGroup))!.GetTableName().Should().Be("social_groups");
        modelBuilder.Model.FindEntityType(typeof(SocialGroupMember))!.GetTableName().Should().Be("social_group_members");

        var module = new SocialGroupsModule();
        module.Name.Should().Be("Social.Groups");
        module.Order.Should().Be(164);
        module.ConfigureServices(new ServiceCollection(), new ConfigurationBuilder().Build()).Should().NotBeNull();
        var endpoints = new FakeEndpointRouteBuilder();
        module.MapEndpoints(endpoints).Should().BeSameAs(endpoints);
    }

    private static (SocialGroupService Service, InMemorySocialGroupRepository Groups, InMemorySocialGroupMemberRepository Members) CreateSeededService(SocialGroupVisibility visibility)
    {
        var groups = new InMemorySocialGroupRepository();
        var members = new InMemorySocialGroupMemberRepository();
        var group = SocialGroup.Create(Guid.NewGuid(), "Seed", "seed", SocialGroupType.StudyGroup, visibility);
        groups.Groups.Add(group);
        members.Members.Add(SocialGroupMember.CreateOwner(group.Id, group.OwnerId));
        return (new SocialGroupService(groups, members), groups, members);
    }

    private static TestSocialGroupsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestSocialGroupsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestSocialGroupsDbContext(options);
    }
}

internal sealed class InMemorySocialGroupRepository : ISocialGroupRepository
{
    public List<SocialGroup> Groups { get; } = [];

    public Task AddAsync(SocialGroup group, CancellationToken cancellationToken = default)
    {
        Groups.Add(group);
        return Task.CompletedTask;
    }

    public Task<SocialGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Groups.FirstOrDefault(group => group.Id == id));

    public Task<SocialGroup?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(Groups.FirstOrDefault(group => group.Slug == slug.ToLowerInvariant()));

    public Task<IReadOnlyList<SocialGroup>> ListAsync(ListSocialGroupsQuery query, CancellationToken cancellationToken = default)
    {
        var groups = Groups.AsEnumerable();

        if (query.Search is not null)
        {
            groups = groups.Where(group => group.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) || group.Slug.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<SocialGroup>>(groups.ToList());
    }

    public Task UpdateAsync(SocialGroup group, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class InMemorySocialGroupMemberRepository : ISocialGroupMemberRepository
{
    public List<SocialGroupMember> Members { get; } = [];

    public Task AddAsync(SocialGroupMember member, CancellationToken cancellationToken = default)
    {
        Members.Add(member);
        return Task.CompletedTask;
    }

    public Task<SocialGroupMember?> GetByGroupUserAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Members.FirstOrDefault(member => member.GroupId == groupId && member.UserId == userId));

    public Task<IReadOnlyList<SocialGroupMember>> ListByGroupAsync(Guid groupId, SocialGroupMembershipStatus? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var members = Members.Where(member => member.GroupId == groupId);
        if (status.HasValue)
        {
            members = members.Where(member => member.Status == status.Value);
        }

        return Task.FromResult<IReadOnlyList<SocialGroupMember>>(members.Skip(skip).Take(take).ToList());
    }

    public Task UpdateAsync(SocialGroupMember member, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class StubSender : ISender
{
    private readonly Dictionary<Type, object?> _responses = [];
    public object? Next { get; set; }
    public object? LastRequest { get; private set; }
    public List<object> Requests { get; } = [];

    public void SetResponse<TRequest>(object? response) => _responses[typeof(TRequest)] = response;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        Requests.Add(request);
        var response = _responses.TryGetValue(request.GetType(), out var configured) ? configured : Next;
        return Task.FromResult((TResponse)response!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
        => Task.CompletedTask;

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        => Task.FromResult(_responses.TryGetValue(request.GetType(), out var configured) ? configured : Next);
}

internal sealed class TestSocialGroupsDbContext(DbContextOptions<TestSocialGroupsDbContext> options) : DbContext(options), IApplicationDbContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => new SocialGroupsModelConfiguration().Configure(modelBuilder);
}

internal sealed class FakeEndpointRouteBuilder : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();

    public ICollection<EndpointDataSource> DataSources { get; } = [];

    public IApplicationBuilder CreateApplicationBuilder() => throw new NotSupportedException();
}
