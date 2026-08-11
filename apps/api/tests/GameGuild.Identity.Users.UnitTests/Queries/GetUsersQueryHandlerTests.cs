using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users.UnitTests.Commands;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IActorContextAccessor> _actorContextAccessorMock;
    private readonly Mock<ITenantMemberRepository> _tenantMemberRepositoryMock;
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _actorContextAccessorMock = new Mock<IActorContextAccessor>();
        _tenantMemberRepositoryMock = new Mock<ITenantMemberRepository>();
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(CreateActorContext());
        _handler = new GetUsersQueryHandler(
            _userRepositoryMock.Object,
            _actorContextAccessorMock.Object,
            _tenantMemberRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldFilterActiveUsers_SearchAndIgnoreInvalidCursor()
    {
        var activeFirst = CreateUser(Guid.Parse("11111111-1111-1111-1111-111111111111"), "zalpha@example.com", "Alpha Two", isActive: true, createdAt: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var activeSecond = CreateUser(Guid.Parse("22222222-2222-2222-2222-222222222222"), "alpha@example.com", "Alpha One", isActive: true, createdAt: new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var inactive = CreateUser(Guid.Parse("33333333-3333-3333-3333-333333333333"), "inactive@example.com", "Alpha Inactive", isActive: false, createdAt: new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        var deleted = CreateDeletedUser(Guid.Parse("44444444-4444-4444-4444-444444444444"), "deleted@example.com", "Alpha Deleted", createdAt: new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc));
        var users = new[] { activeFirst, activeSecond, inactive, deleted };
        var query = new GetUsersQuery(
            Status: "active",
            IncludeDeleted: false,
            SearchTerm: "alpha",
            Cursor: "not-a-valid-base64-cursor",
            Limit: 10,
            Sort: "-email");

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(users));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.Skip.Should().Be(0);
        result.Items.Select(item => item.Email).Should().Equal("zalpha@example.com", "alpha@example.com");
    }

    [Fact]
    public async Task Handle_WithValidCursor_ShouldReturnOnlyUsersAfterCursor()
    {
        var first = CreateUser(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "first@example.com", "First", isActive: true, createdAt: new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var middle = CreateUser(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "middle@example.com", "Middle", isActive: true, createdAt: new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc));
        var last = CreateUser(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "last@example.com", "Last", isActive: true, createdAt: new DateTime(2024, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        var cursor = GetUsersQueryHandler.EncodeCursor(middle.CreatedAt, middle.Id);
        var query = new GetUsersQuery(Cursor: cursor, Limit: 10, Sort: "created_at");

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(new[] { first, middle, last }));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(last.Id);
    }

    [Fact]
    public async Task Handle_WithDeletedStatusAndIncludeDeleted_ShouldReturnDeletedUsers()
    {
        var deleted = CreateDeletedUser(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "deleted@example.com", "Deleted User", createdAt: new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        var active = CreateUser(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "active@example.com", "Active User", isActive: true, createdAt: new DateTime(2024, 3, 2, 0, 0, 0, DateTimeKind.Utc));
        var query = new GetUsersQuery(Status: "deleted", IncludeDeleted: true, Limit: 10, Sort: "name");

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(new[] { deleted, active }));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(deleted.Id);
        result.Items[0].Email.Should().Be("deleted@example.com");
    }

    [Fact]
    public async Task Handle_WithInactiveStatusAndEmailFilter_ShouldMatchCaseInsensitiveEmail()
    {
        var inactiveMatch = CreateUser(Guid.Parse("11111111-2222-3333-4444-555555555555"), "match@example.com", "Match User", isActive: false, createdAt: new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var activeSameName = CreateUser(Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"), "other@example.com", "Match User", isActive: true, createdAt: new DateTime(2024, 4, 2, 0, 0, 0, DateTimeKind.Utc));
        var query = new GetUsersQuery(Email: "MATCH@example.com", Status: "inactive", IncludeDeleted: false, Limit: 10, Sort: "updated_at");

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(new[] { inactiveMatch, activeSameName }));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(inactiveMatch.Id);
    }

    [Fact]
    public async Task Handle_WithMalformedButBase64Cursor_ShouldIgnoreCursorAndReturnSortedUsers()
    {
        var first = CreateUser(Guid.Parse("10101010-1010-1010-1010-101010101010"), "first@example.com", "First", isActive: true, createdAt: new DateTime(2024, 4, 10, 0, 0, 0, DateTimeKind.Utc));
        var second = CreateUser(Guid.Parse("20202020-2020-2020-2020-202020202020"), "second@example.com", "Second", isActive: true, createdAt: new DateTime(2024, 4, 11, 0, 0, 0, DateTimeKind.Utc));
        var malformedCursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("missing-delimiter"));
        var query = new GetUsersQuery(Cursor: malformedCursor, Limit: 10, Sort: "created_at");

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(new[] { second, first }));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Select(item => item.Id).Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Handle_ShouldCoverRemainingSortDirections()
    {
        var alpha = CreateUser(Guid.Parse("30303030-3030-3030-3030-303030303030"), "charlie@example.com", "Alpha", isActive: true, createdAt: new DateTime(2024, 4, 20, 0, 0, 0, DateTimeKind.Utc));
        alpha.UpdatedAt = new DateTime(2024, 4, 22, 0, 0, 0, DateTimeKind.Utc);

        var bravo = CreateUser(Guid.Parse("40404040-4040-4040-4040-404040404040"), "alpha@example.com", "Bravo", isActive: true, createdAt: new DateTime(2024, 4, 21, 0, 0, 0, DateTimeKind.Utc));
        bravo.UpdatedAt = new DateTime(2024, 4, 24, 0, 0, 0, DateTimeKind.Utc);

        var charlie = CreateUser(Guid.Parse("50505050-5050-5050-5050-505050505050"), "bravo@example.com", "Charlie", isActive: true, createdAt: new DateTime(2024, 4, 22, 0, 0, 0, DateTimeKind.Utc));
        charlie.UpdatedAt = new DateTime(2024, 4, 23, 0, 0, 0, DateTimeKind.Utc);

        var users = new[] { alpha, bravo, charlie };
        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(() => new TestAsyncEnumerable<User>(users));

        var emailAscending = await _handler.Handle(new GetUsersQuery(Limit: 10, Sort: "email"), CancellationToken.None);
        var nameDescending = await _handler.Handle(new GetUsersQuery(Limit: 10, Sort: "-name"), CancellationToken.None);
        var updatedAtDescending = await _handler.Handle(new GetUsersQuery(Limit: 10, Sort: "-updated_at"), CancellationToken.None);

        emailAscending.Items.Select(item => item.Email).Should().Equal("alpha@example.com", "bravo@example.com", "charlie@example.com");
        nameDescending.Items.Select(item => item.Name).Should().Equal("Charlie", "Bravo", "Alpha");
        updatedAtDescending.Items.Select(item => item.Name).Should().Equal("Bravo", "Charlie", "Alpha");
    }

    [Fact]
    public async Task Handle_WithUnknownStatusAndNoSort_ShouldFallbackToCreatedAtAscending()
    {
        var first = CreateUser(Guid.Parse("12121212-1212-1212-1212-121212121212"), "first@example.com", "First", isActive: true, createdAt: new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        var second = CreateUser(Guid.Parse("23232323-2323-2323-2323-232323232323"), "second@example.com", "Second", isActive: true, createdAt: new DateTime(2024, 5, 2, 0, 0, 0, DateTimeKind.Utc));
        var deleted = CreateDeletedUser(Guid.Parse("34343434-3434-3434-3434-343434343434"), "deleted@example.com", "Deleted", createdAt: new DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc));
        var query = new GetUsersQuery(Status: "unknown", IncludeDeleted: true, Limit: 10, Sort: null);

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(new[] { second, deleted, first }));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Select(item => item.Id).Should().Equal(first.Id, second.Id, deleted.Id);
    }

    [Fact]
    public async Task Handle_WithDescendingCursor_ShouldReturnOnlyOlderUsers()
    {
        var oldest = CreateUser(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"), "oldest@example.com", "Oldest", isActive: true, createdAt: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var middle = CreateUser(Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"), "middle@example.com", "Middle", isActive: true, createdAt: new DateTime(2024, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        var newest = CreateUser(Guid.Parse("cccccccc-3333-3333-3333-333333333333"), "newest@example.com", "Newest", isActive: true, createdAt: new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc));
        var cursor = GetUsersQueryHandler.EncodeCursor(middle.CreatedAt, middle.Id);
        var query = new GetUsersQuery(Cursor: cursor, Limit: 10, Sort: "-created_at");

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(new[] { oldest, middle, newest }));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(oldest.Id);
    }

    [Fact]
    public async Task Handle_WithSystemAdminTenantContext_ShouldReturnGlobalResults()
    {
        var activeTenantId = Guid.Parse("f1d53fe7-e6fb-4712-8b55-b37d2d0ed701");
        var otherTenantId = Guid.Parse("9f54d387-4080-49f4-8b0f-60c09b90d6a1");

        var activeTenantUser = CreateUser(Guid.Parse("61616161-6161-6161-6161-616161616161"), "active-tenant@example.com", "Active Tenant User", isActive: true, createdAt: new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        activeTenantUser.TenantMemberships.Add(CreateTenantMembership(activeTenantUser.Id, activeTenantId));

        var otherTenantUser = CreateUser(Guid.Parse("71717171-7171-7171-7171-717171717171"), "other-tenant@example.com", "Other Tenant User", isActive: true, createdAt: new DateTime(2024, 7, 2, 0, 0, 0, DateTimeKind.Utc));
        otherTenantUser.TenantMemberships.Add(CreateTenantMembership(otherTenantUser.Id, otherTenantId));

        _actorContextAccessorMock
            .Setup(x => x.ActorContext)
            .Returns(CreateActorContext(activeTenantId));
        _userRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new TestAsyncEnumerable<User>(new[] { activeTenantUser, otherTenantUser }));

        var result = await _handler.Handle(new GetUsersQuery(Limit: 10, Sort: "email"), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Select(item => item.Id).Should().Contain(activeTenantUser.Id).And.Contain(otherTenantUser.Id);
        _tenantMemberRepositoryMock.Verify(
            x => x.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static User CreateUser(Guid id, string email, string name, bool isActive, DateTime createdAt)
    {
        var user = User.Create(email, name);
        user.Id = id;
        user.CreatedAt = createdAt;
        user.UpdatedAt = createdAt.AddHours(1);

        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }

    private static User CreateDeletedUser(Guid id, string email, string name, DateTime createdAt)
    {
        var user = CreateUser(id, email, name, isActive: true, createdAt);
        user.Version = 1;
        user.MarkDeleted();
        user.CreatedAt = createdAt;
        user.UpdatedAt = createdAt.AddHours(1);
        user.DeletedAt = createdAt.AddHours(2);
        return user;
    }

    private static TenantMember CreateTenantMembership(Guid userId, Guid tenantId)
        => new()
        {
            UserId = userId,
            TenantId = tenantId,
            Role = "Member",
            IsActive = true
        };

    private static ActorContext CreateActorContext(Guid? tenantId = null)
        => new()
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.Parse("81818181-8181-8181-8181-818181818181").ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string> { "SystemAdmin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        };
}
