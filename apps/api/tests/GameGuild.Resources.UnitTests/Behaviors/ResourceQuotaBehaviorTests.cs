using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Behaviors;

public class ResourceQuotaBehaviorTests
{
    private readonly Mock<IResourceQuotaService> _quotaServiceMock;
    private readonly Mock<IActorContextAccessor> _actorContextAccessorMock;
    private readonly Mock<ILogger<ResourceQuotaBehavior<TestQuotaCommand, Unit>>> _loggerMock;
    private readonly ResourceQuotaBehavior<TestQuotaCommand, Unit> _behavior;
    private readonly Mock<RequestHandlerDelegate<Unit>> _nextMock;

    public ResourceQuotaBehaviorTests()
    {
        _quotaServiceMock = new Mock<IResourceQuotaService>();
        _actorContextAccessorMock = new Mock<IActorContextAccessor>();
        _loggerMock = new Mock<ILogger<ResourceQuotaBehavior<TestQuotaCommand, Unit>>>();
        _behavior = new ResourceQuotaBehavior<TestQuotaCommand, Unit>(
            _quotaServiceMock.Object,
            _actorContextAccessorMock.Object,
            _loggerMock.Object);
        _nextMock = new Mock<RequestHandlerDelegate<Unit>>();
        _nextMock.Setup(x => x()).ReturnsAsync(Unit.Value);
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenTenantIdMissing()
    {
        // Arrange
        var command = new TestQuotaCommand();
        var actorContext = ActorContext.Anonymous; // No tenant ID
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(actorContext);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(command, _nextMock.Object, CancellationToken.None));

        exception.Message.Should().Contain("requires tenant context");
        exception.Message.Should().Contain("X-Tenant-Id");
        
        // Verify next handler was never called (fail-closed)
        _nextMock.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenQuotaServiceFails()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new TestQuotaCommand();
        var actorContext = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(actorContext);

        var expectedException = new InvalidOperationException("Database connection failed");
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(
                tenantId,
                ResourceUsageType.Users,
                1L,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(command, _nextMock.Object, CancellationToken.None));

        exception.Should().Be(expectedException);
        exception.Message.Should().Contain("Database connection failed");
        
        // Verify next handler was never called (fail-closed on quota service error)
        _nextMock.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_AllowsProceed_WhenNoQuotaAttributePresent()
    {
        // Arrange
        var command = new TestCommand();
        var actorContext = ActorContext.Anonymous; // No tenant, but shouldn't matter
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(actorContext);

        // Act
        var result = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _nextMock.Verify(x => x(), Times.Once);
        _quotaServiceMock.Verify(
            x => x.TryAtomicConsumeAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ConsumesQuota_WhenRecordUsageIsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new TestQuotaCommand();
        var actorContext = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(actorContext);

        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(
                tenantId,
                ResourceUsageType.Users,
                1L,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 5L, 10L));

        // Act
        var result = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _nextMock.Verify(x => x(), Times.Once);
        _quotaServiceMock.Verify(
            x => x.TryAtomicConsumeAsync(tenantId, ResourceUsageType.Users, 1L, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsQuotaExceeded_WhenAtomicConsumeFails()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new TestQuotaCommand();
        var actorContext = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(actorContext);

        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(
                tenantId,
                ResourceUsageType.Users,
                1L,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 10L, 10L)); // At limit

        // Act & Assert
        var exception = await Assert.ThrowsAsync<QuotaExceededException>(
            () => _behavior.Handle(command, _nextMock.Object, CancellationToken.None));

        exception.Message.Should().Contain("Resource quota exceeded");
        exception.Message.Should().Contain("Users");
        exception.Message.Should().Contain("10"); // Current usage
        
        // Verify next handler was never called
        _nextMock.Verify(x => x(), Times.Never);
    }

    // Test command without RequiresQuota attribute
    private class TestCommand : IRequest<Unit>
    {
    }

    // Test command with RequiresQuota attribute
    [RequiresQuota(ResourceUsageType.Users, 1)]
    private class TestQuotaCommand : IRequest<Unit>
    {
    }
}
