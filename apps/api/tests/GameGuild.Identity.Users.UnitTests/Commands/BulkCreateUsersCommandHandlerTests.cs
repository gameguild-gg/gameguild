using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkCreateUsersCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IResourceQuotaService> _quotaServiceMock;
    private readonly Mock<IActorContextAccessor> _actorContextAccessorMock;
    private readonly BulkCreateUsersCommandHandler _handler;

    public BulkCreateUsersCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _quotaServiceMock = new Mock<IResourceQuotaService>();
        _actorContextAccessorMock = new Mock<IActorContextAccessor>();
        
        // Default: no tenant context (quota checks skipped)
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(ActorContext.Anonymous);
        
        _handler = new BulkCreateUsersCommandHandler(
            _userRepositoryMock.Object,
            _quotaServiceMock.Object,
            _actorContextAccessorMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUsers_ShouldCreateAllUsers()
    {
        // Arrange
        var userRequests = new List<CreateUserRequestItem>
        {
            new("user1@test.com", "User One", null),
            new("user2@test.com", "User Two", "+1234567890"),
            new("user3@test.com", "User Three", null)
        };
        var command = new BulkCreateUsersCommand(userRequests);

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedUserIds.Should().HaveCount(3);
        result.FailedEmails.Should().BeEmpty();

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        _userRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmails_ShouldFailForDuplicates()
    {
        // Arrange
        var existingUser = User.Create("existing@test.com", "Existing User", null);
        var userRequests = new List<CreateUserRequestItem>
        {
            new("existing@test.com", "Duplicate User", null),
            new("new@test.com", "New User", null)
        };
        var command = new BulkCreateUsersCommand(userRequests);

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existingUser });

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedUserIds.Should().HaveCount(1);
        result.FailedEmails.Should().Contain("existing@test.com");
        result.FailedEmails.Should().NotContain("new@test.com");

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyList_ShouldReturnEmptyResult()
    {
        // Arrange
        var command = new BulkCreateUsersCommand(Array.Empty<CreateUserRequestItem>());

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedUserIds.Should().BeEmpty();
        result.FailedEmails.Should().BeEmpty();

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithTenantQuotaExceeded_ShouldThrowQuotaExceededException()
    {
        var tenantId = Guid.NewGuid();
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(CreateTenantActorContext(tenantId));

        var command = new BulkCreateUsersCommand(
            new[]
            {
                new CreateUserRequestItem("user1@test.com", "User One", null),
                new CreateUserRequestItem("user2@test.com", "User Two", null)
            });

        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(tenantId, ResourceUsageType.Users, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 10L, 10L));

        await Assert.ThrowsAsync<QuotaExceededException>(() => _handler.Handle(command, CancellationToken.None));

        _userRepositoryMock.Verify(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithTenantQuotaAndFailedItems_ShouldAdjustQuotaToActualCreatedCount()
    {
        var tenantId = Guid.NewGuid();
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(CreateTenantActorContext(tenantId));

        var existingUser = User.Create("existing@test.com", "Existing User", null);
        var command = new BulkCreateUsersCommand(
            new[]
            {
                new CreateUserRequestItem("existing@test.com", "Duplicate User", null),
                new CreateUserRequestItem("broken@test.com", string.Empty, null),
                new CreateUserRequestItem("new@test.com", "New User", null)
            });

        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(tenantId, ResourceUsageType.Users, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0L, 10L));
        _quotaServiceMock
            .Setup(x => x.DecrementUsageAsync(tenantId, ResourceUsageType.Users, 2, null, "BulkCreateUsers:Adjustment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existingUser });
        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.CreatedUserIds.Should().HaveCount(1);
        result.FailedEmails.Should().BeEquivalentTo(["existing@test.com", "broken@test.com"]);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(tenantId, ResourceUsageType.Users, 2, null, "BulkCreateUsers:Adjustment", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFailsAfterQuotaReservation_ShouldRollbackReservedQuota()
    {
        var tenantId = Guid.NewGuid();
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(CreateTenantActorContext(tenantId));

        var command = new BulkCreateUsersCommand(
            new[]
            {
                new CreateUserRequestItem("user1@test.com", "User One", null),
                new CreateUserRequestItem("user2@test.com", "User Two", null)
            });

        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(tenantId, ResourceUsageType.Users, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0L, 10L));
        _quotaServiceMock
            .Setup(x => x.DecrementUsageAsync(tenantId, ResourceUsageType.Users, 2, null, "BulkCreateUsers:Rollback", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(tenantId, ResourceUsageType.Users, 2, null, "BulkCreateUsers:Rollback", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ActorContext CreateTenantActorContext(Guid tenantId)
    {
        return new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        };
    }
}
