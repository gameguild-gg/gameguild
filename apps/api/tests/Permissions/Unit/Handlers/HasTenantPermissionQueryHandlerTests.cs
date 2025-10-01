using FluentAssertions;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Handlers;

/// <summary>
/// Unit tests for the HasTenantPermissionQueryHandler
/// Tests permission checking logic, caching, and error conditions
/// </summary>
public class HasTenantPermissionQueryHandlerTests
{
    private readonly Mock<ICachedPermissionService> _mockPermissionService;
    private readonly Mock<ILogger<HasTenantPermissionQueryHandler>> _mockLogger;
    private readonly HasTenantPermissionQueryHandler _handler;

    public HasTenantPermissionQueryHandlerTests()
    {
        _mockPermissionService = new Mock<ICachedPermissionService>();
        _mockLogger = new Mock<ILogger<HasTenantPermissionQueryHandler>>();
        _handler = new HasTenantPermissionQueryHandler(_mockPermissionService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenUserHasPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        _mockPermissionService.Setup(s => s.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read))
                             .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockPermissionService.Verify(s => s.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenUserDoesNotHavePermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Comment
        };

        _mockPermissionService.Setup(s => s.HasTenantPermissionAsync(userId, tenantId, PermissionType.Comment))
                             .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPermissionService.Verify(s => s.HasTenantPermissionAsync(userId, tenantId, PermissionType.Comment), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenNoPermissionRecordExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        _mockPermissionService.Setup(s => s.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read))
                             .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPermissionService.Verify(s => s.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read), Times.Once);
    }

    [Theory]
    [InlineData(PermissionType.Read)]
    [InlineData(PermissionType.Comment)]
    [InlineData(PermissionType.Vote)]
    [InlineData(PermissionType.Admin)]
    public async Task Handle_ShouldWorkForAllPermissionTypes(PermissionType permissionType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = permissionType
        };

        _mockPermissionService.Setup(s => s.HasTenantPermissionAsync(userId, tenantId, permissionType))
                             .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockPermissionService.Verify(s => s.HasTenantPermissionAsync(userId, tenantId, permissionType), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPermissionServiceIsNull()
    {
        // Act & Assert
        var action = () => new HasTenantPermissionQueryHandler(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("permissionService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var action = () => new HasTenantPermissionQueryHandler(_mockPermissionService.Object, null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("logger");
    }
}