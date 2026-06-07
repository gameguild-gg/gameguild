using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants.UnitTests.Infrastructure;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IActorContextAccessor> _actorContextAccessorMock;
    private readonly TestTenantDbContext _dbContext;
    private readonly CreateTenantCommandHandler _handler;
    private readonly Guid _actorUserId = Guid.NewGuid();

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _actorContextAccessorMock = new Mock<IActorContextAccessor>();

        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"CreateTenantCommandHandlerTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TestTenantDbContext(options);
        _actorContextAccessorMock
            .SetupGet(x => x.ActorContext)
            .Returns(new ActorContext
            {
                ActorKind = ActorKind.User,
                SubjectId = _actorUserId.ToString(),
                TenantId = null,
                Roles = new HashSet<string>(),
                Permissions = new HashSet<string>(),
                TypedAttributes = ActorAttributes.Empty,
                AuthScheme = "Test",
                IsAuthenticated = true
            });

        _handler = new CreateTenantCommandHandler(
            _tenantRepositoryMock.Object,
            _actorContextAccessorMock.Object,
            _dbContext);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateTenant()
    {
        // Arrange
        var command = new CreateTenantCommand(
            "Test Tenant",
            "test-tenant",
            "admin@test.com",
            "Test description");

        _tenantRepositoryMock.Setup(x => x.IsSlugUniqueAsync("test-tenant", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        var createdTenant = _dbContext.Tenants.Single();
        var createdMembership = _dbContext.TenantMembers.Single();

        // Assert
        result.Should().Be(createdTenant.Id);
        createdTenant.Name.Should().Be("Test Tenant");
        createdTenant.Slug.Should().Be("test-tenant");
        createdTenant.AdminEmail.Should().Be("admin@test.com");
        createdTenant.IsActive.Should().BeTrue();
        createdMembership.TenantId.Should().Be(createdTenant.Id);
        createdMembership.UserId.Should().Be(_actorUserId);
        createdMembership.Role.Should().Be(TenantRole.Owner);
        createdMembership.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDuplicateSlug_ShouldThrowException()
    {
        // Arrange
        var command = new CreateTenantCommand(
            "Test Tenant",
            "existing-slug",
            "admin@test.com",
            null);

        _tenantRepositoryMock.Setup(x => x.IsSlugUniqueAsync("existing-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        _dbContext.Tenants.Should().BeEmpty();
        _dbContext.TenantMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAllParameters_ShouldCreateTenantWithDescription()
    {
        // Arrange
        var command = new CreateTenantCommand(
            "Full Tenant",
            "full-tenant",
            "admin@full.com",
            "Full description");

        _tenantRepositoryMock.Setup(x => x.IsSlugUniqueAsync("full-tenant", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        var capturedTenant = _dbContext.Tenants.Single();
        var capturedMembership = _dbContext.TenantMembers.Single();

        // Assert
        result.Should().Be(capturedTenant.Id);
        capturedTenant.Name.Should().Be("Full Tenant");
        capturedTenant.Slug.Should().Be("full-tenant");
        capturedTenant.AdminEmail.Should().Be("admin@full.com");
        capturedTenant.Description.Should().Be("Full description");
        capturedTenant.IsActive.Should().BeTrue();
        capturedMembership.TenantId.Should().Be(capturedTenant.Id);
        capturedMembership.UserId.Should().Be(_actorUserId);
        capturedMembership.Role.Should().Be(TenantRole.Owner);
    }
}
